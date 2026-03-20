using Ax.Fw.Extensions;
using Ax.Fw.Log;
using Ax.Fw.SharedTypes.Data.Log;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Web.Data.WsServer;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Net.WebSockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ax.Fw.Web.Modules.WebSocketServer;

/// <summary>
/// Provides a server for managing WebSocket connections, message broadcasting, and session grouping with typed session
/// groups.
/// </summary>
/// <remarks>The WebSocketServer enables real-time communication between clients by handling connection lifecycle
/// events, message serialization, and broadcasting within session groups. It supports typed message handling and
/// integrates with .NET's reactive streams for event notification. Thread safety is ensured for session management and
/// broadcasting operations. Use this class to build scalable WebSocket-based applications where clients are organized
/// into logical groups.</remarks>
/// <typeparam name="TClientData">The type used to store some client-specific data.</typeparam>
/// <typeparam name="TClientGroup">The type used to group WebSocket clients.</typeparam>
public class WebSocketServer<TClientData, TClientGroup>
  where TClientData : notnull, IEquatable<TClientData>
  where TClientGroup : notnull, IEquatable<TClientGroup>
{
  private sealed record WsMsg(Guid ConnectionId, TClientData Sender, TClientGroup ClientGroup, object Msg);
  private sealed record BroadcastTask(TClientGroup ClientGroup, byte[] Data, bool Compressed);
  private sealed record SpecificUserTask(TClientGroup ClientGroup, TClientData ClientData, byte[] Data, bool Compressed);

  private readonly IReadOnlyLifetime p_lifetime;
  private readonly ILog p_log;
  private readonly IReadOnlyDictionary<string, Type> p_msgTypes;
  private readonly IReadOnlyDictionary<Type, string> p_msgTypesReverse;
  private readonly JsonSerializerContext p_jsonCtx;
  private readonly Subject<WsMsg> p_incomingMsgs = new();
  private readonly Subject<WebSocketSession<TClientData, TClientGroup>> p_clientConnectedFlow = new();
  private readonly Subject<WebSocketSession<TClientData, TClientGroup>> p_clientDisconnectedFlow = new();
  private readonly Subject<BroadcastTask> p_broadcastQueueSubj = new();
  private readonly Subject<SpecificUserTask> p_specificUserQueueSubj = new();
  private readonly TimeSpan p_connectionMaxIdleTime;
  private readonly ConcurrentDictionary<Guid, WebSocketSession<TClientData, TClientGroup>> p_sessions = new();

  /// <summary>
  /// Initializes a new instance of the WebSocketServer class.
  /// </summary>
  /// <param name="_lifetime">The lifetime scope that controls the disposal and shutdown of the server and its resources.</param>
  /// <param name="_log">The logger instance used for logging server operations and errors.</param>
  /// <param name="_jsonCtx">The JSON serializer context used for serializing and deserializing messages. Must include serialization of type <see cref="WsBaseMsg"></param>
  /// <param name="_msgTypes">A read-only dictionary mapping message type names to their corresponding .NET types. Defines the set of supported
  /// message types for the server.</param>
  /// <param name="_connectionMaxIdleTime">The maximum duration that a connection can remain idle before being closed.</param>
  /// <param name="_onError">An optional callback invoked with an error message when an error occurs during server operations, such as
  /// broadcasting messages.</param>
  /// <exception cref="ArgumentException">Thrown if the provided JSON serializer context is not of type WebSocketServerJsonCtx.</exception>
  public WebSocketServer(
    IReadOnlyLifetime _lifetime,
    ILog _log,
    JsonSerializerContext _jsonCtx,
    IReadOnlyDictionary<string, Type> _msgTypes,
    TimeSpan _connectionMaxIdleTime)
  {
    if (_jsonCtx.GetTypeInfo(typeof(WsBaseMsg)) == null)
      throw new ArgumentException($"Json context should contain type info for '{typeof(WsBaseMsg)}'", nameof(_jsonCtx));

    p_lifetime = _lifetime;
    p_jsonCtx = _jsonCtx;
    p_msgTypes = _msgTypes;

    var msgTypesReverse = new Dictionary<Type, string>();
    foreach (var entry in _msgTypes)
      msgTypesReverse[entry.Value] = entry.Key;
    p_msgTypesReverse = msgTypesReverse;

    p_connectionMaxIdleTime = _connectionMaxIdleTime;
    p_log = _log;

    var postScheduler = _lifetime.ToDisposeOnEnded(new EventLoopScheduler());
    p_broadcastQueueSubj
      .ObserveOn(postScheduler)
      .SelectAsync(async (_msg, _ct) =>
      {
        try
        {
          await BroadcastMsgAsync(_msg.ClientGroup, _msg.Data, _msg.Compressed, _ct);
        }
        catch (Exception ex)
        {
          p_log.Error($"Can't broadcast msg from post queue: {ex}");
        }
      }, postScheduler)
      .Subscribe(_lifetime);

    p_specificUserQueueSubj
      .ObserveOn(postScheduler)
      .SelectAsync(async (_msg, _ct) =>
      {
        var sessions = p_sessions.Values
          .Where(_ => _.ClientGroup.Equals(_msg.ClientGroup) && _.ClientData.Equals(_msg.ClientData))
          .ToArray();

        foreach (var session in sessions)
        {
          try
          {
            await SendMsgUnsafeAsync(session, _msg.Compressed, _msg.Data, _ct);
          }
          catch (Exception ex)
          {
            p_log.Error($"Can't send msg to client {session.ClientGroup}/{session.ClientData}: {ex}");
          }
        }
      }, postScheduler)
      .Subscribe(_lifetime);
  }

  /// <summary>
  /// Gets an observable sequence that signals when a new client establishes a WebSocket connection.
  /// </summary>
  public IObservable<WebSocketSession<TClientData, TClientGroup>> ClientConnected => p_clientConnectedFlow;

  /// <summary>
  /// Gets an observable sequence that signals when a client disconnects from a WebSocket session.
  /// </summary>
  public IObservable<WebSocketSession<TClientData, TClientGroup>> ClientDisconnected => p_clientDisconnectedFlow;

  /// <summary>
  /// Gets a read-only list of all active WebSocket sessions.
  /// </summary>
  public IReadOnlyList<WebSocketSession<TClientData, TClientGroup>> Sessions => [.. p_sessions.Values];

  /// <summary>
  /// Accepts a WebSocket connection and initializes a new session asynchronously.
  /// </summary>
  /// <remarks>If the provided WebSocket is not open, the method returns <see langword="false"/> immediately.
  /// Otherwise, it creates a new session and waits for session to end.</remarks>
  /// <param name="_clientData">The client data for the client.</param>
  /// <param name="_clientGroup">The group associated with the new WebSocket session.</param>
  /// <param name="_webSocket">The WebSocket instance to be accepted. Must be in the <see cref="WebSocketState.Open"/> state.</param>
  public async Task<bool> AcceptSocketAsync(
    TClientData _clientData,
    TClientGroup _clientGroup,
    WebSocket _webSocket)
  {
    if (_webSocket.State != WebSocketState.Open)
      return false;

    using var life = p_lifetime.GetChildLifetime()
      ?? throw new InvalidOperationException("Failed to create child lifetime for WebSocket session.");

    using var session = new WebSocketSession<TClientData, TClientGroup>(Guid.NewGuid(), _clientData, _clientGroup, _webSocket);
    using var semaphore = new SemaphoreSlim(0, 1);
    using var scheduler = new EventLoopScheduler();

    scheduler.ScheduleAsync(async (_s, _ct) => await CreateNewLoopAsync(session, semaphore));

    try
    {
      await semaphore.WaitAsync(p_lifetime.Token);
    }
    catch (OperationCanceledException) { }
    catch (Exception ex)
    {
      p_log.Error($"Error in message loop of session '{session.ConnectionId}': {ex}");
    }

    return true;
  }

  /// <summary>
  /// Returns an observable sequence of incoming messages of the specified type that belong to the given session group.
  /// </summary>
  /// <remarks>The returned sequence excludes messages that do not match the specified type or session group.</remarks>
  /// <typeparam name="T">The message type to filter for. Must be a reference type.</typeparam>
  /// <param name="_group">The group to filter incoming messages by. Only messages associated with this group are included in the
  /// sequence.</param>
  /// <returns>An observable sequence containing messages of type T from the specified session group. The sequence emits only
  /// messages that match the type and group criteria.</returns>
  public IObservable<IncomingWsMsg<TClientData, TClientGroup, TData>> IncomingMessagesOfType<TData>(TClientGroup _group)
    where TData : class
  {
    return p_incomingMsgs
      .Where(_ => _.ClientGroup.Equals(_group))
      .Select(_ =>
      {
        if (_.Msg is TData typedMsg)
          return new IncomingWsMsg<TClientData, TClientGroup, TData>(_.ConnectionId, _.Sender, _.ClientGroup, typedMsg);
        else
          return null;
      })
      .WhereNotNull();
  }

  /// <summary>
  /// Broadcasts a message to all sessions in the specified session group asynchronously.
  /// </summary>
  /// <typeparam name="T">The type of the message to broadcast. Must not be null.</typeparam>
  /// <param name="_sessionGroup">The group to which the message will be broadcast.</param>
  /// <param name="_msg">The message to broadcast to all sessions in the group. Cannot be null.</param>
  /// <param name="_compress">A value indicating whether the message should be compressed before broadcasting. If <see langword="true"/>, the
  /// message will be compressed.</param>
  /// <param name="_ct">A cancellation token that can be used to cancel the broadcast operation.</param>
  /// <returns>A task that represents the asynchronous operation. The task result contains the number of sessions to which the
  /// message was successfully broadcast.</returns>
  public async Task<int> BroadcastMsgAsync<T>(
    TClientGroup _sessionGroup,
    T _msg,
    bool _compress,
    CancellationToken _ct) where T : notnull
  {
    var buffer = CreateWsMessage(_msg, _compress);

    return await BroadcastMsgAsync(_sessionGroup, buffer, _compress, _ct);
  }

  /// <summary>
  /// Broadcasts a message to all sessions in the specified session group asynchronously.
  /// </summary>
  /// <param name="_sessionGroup">The group to which the message will be broadcast.</param>
  /// <param name="_msg">The message data to send to each session, as a byte array.</param>
  /// <param name="_asBinary">Indicates whether the message should be sent as binary (<see langword="true"/>) or as text (<see
  /// langword="false"/>).</param>
  /// <param name="_ct">A cancellation token that can be used to cancel the broadcast operation.</param>
  /// <returns>The number of sessions in the group that successfully received the message.</returns>
  public async Task<int> BroadcastMsgAsync(
    TClientGroup _sessionGroup,
    byte[] _msg,
    bool _asBinary,
    CancellationToken _ct)
  {
    var totalSent = 0;
    var sessionsToSend = p_sessions
      .Where(_ => _.Value.ClientGroup.Equals(_sessionGroup))
      .Select(_ => _.Value)
      .ToArray();

    await Parallel.ForEachAsync(sessionsToSend, _ct, async (_session, _c) =>
    {
      try
      {
        var sent = await SendMsgUnsafeAsync(_session, _asBinary, _msg, _c);
        if (sent)
          Interlocked.Increment(ref totalSent);
      }
      catch (Exception ex)
      {
        p_log.Error($"Can't send msg to session '{_session.ConnectionId} / {_session.ClientGroup} / {_session.ClientData}': {ex}");
      }
    });

    return totalSent;
  }

  /// <summary>
  /// Posts a broadcast message to all sessions in the specified session group.
  /// </summary>
  /// <remarks>This method enqueues the broadcast message for asynchronous delivery to all sessions in the group.</remarks>
  /// <typeparam name="T">The type of the message to broadcast.</typeparam>
  /// <param name="_clientGroup">The group to which the broadcast message will be sent.</param>
  /// <param name="_msg">The message to broadcast to all sessions in the group.</param>
  /// <param name="_compress">Indicates whether the message should be compressed before broadcasting. Set to <see langword="true"/> to enable
  /// compression; otherwise, <see langword="false"/>.</param>
  public void PostBroadcastMsg<T>(
    TClientGroup _clientGroup,
    T _msg,
    bool _compress = false) where T : notnull
  {
    var buffer = CreateWsMessage(_msg, _compress);
    p_broadcastQueueSubj.OnNext(new BroadcastTask(_clientGroup, buffer, _compress));
  }

  /// <summary>
  /// Asynchronously sends a message to the specified WebSocket session, optionally compressing the message before transmission.
  /// </summary>
  /// <typeparam name="T">The type of the message to send.</typeparam>
  /// <param name="_session">The WebSocket session to which the message will be sent.</param>
  /// <param name="_msg">The message to send to the WebSocket session.</param>
  /// <param name="_compress">Indicates whether the message should be sent in compressed binary format (<see langword="true"/>) or as plain text
  /// (<see langword="false"/>).</param>
  /// <param name="_ct">A cancellation token that can be used to cancel the send operation.</param>
  public async Task SendMsgAsync<T>(
    WebSocketSession<TClientData, TClientGroup> _session,
    T _msg,
    bool _compress,
    CancellationToken _ct) where T : notnull
  {
    var buffer = CreateWsMessage(_msg, _compress);

    try
    {
      await SendMsgUnsafeAsync(_session, _compress, buffer, _ct);
    }
    catch (Exception ex)
    {
      p_log.Error($"Can't send msg to session '{_session.ConnectionId} / {_session.ClientGroup} / {_session.ClientData}': {ex}");
    }
  }

  /// <summary>
  /// Asynchronously sends a message to each WebSocket session in the specified collection.
  /// </summary>
  /// <typeparam name="T">The type of the message to send.</typeparam>
  /// <param name="_sessions">The collection of WebSocket sessions to which the message will be sent.</param>
  /// <param name="_msg">The message to send to each session.</param>
  /// <param name="_compress">Indicates whether the message should be sent in compressed binary format (<see langword="true"/>) or as plain text
  /// (<see langword="false"/>).</param>
  /// <param name="_ct">A cancellation token that can be used to cancel the send operation.</param>
  /// <returns>The number of clients to which the message was successfully sent.</returns>
  public async Task<int> SendMsgAsync<T>(
    IEnumerable<WebSocketSession<TClientData, TClientGroup>> _sessions,
    T _msg,
    bool _compress,
    CancellationToken _ct)
    where T : notnull
  {
    var buffer = CreateWsMessage(_msg, _compress);
    var totalSent = 0;

    await Parallel.ForEachAsync(_sessions, _ct, async (_session, _c) =>
    {
      try
      {
        var sent = await SendMsgUnsafeAsync(_session, _compress, buffer, _c);
        if (sent)
          Interlocked.Increment(ref totalSent);
      }
      catch (Exception ex)
      {
        p_log.Error($"Can't send msg to session '{_session.ConnectionId} / {_session.ClientGroup} / {_session.ClientData}': {ex}");
      }
    });

    return totalSent;
  }

  /// <summary>
  /// Posts a message to a specific client group and client data, optionally compressing the message before sending.
  /// </summary>
  /// <typeparam name="T">The type of the message to be sent. Must not be null.</typeparam>
  /// <param name="_clientGroup">The client group that identifies the set of clients to which the message will be sent.</param>
  /// <param name="_clientData">The client-specific data used to further target the message delivery.</param>
  /// <param name="_msg">The message to send. Cannot be null.</param>
  /// <param name="_compress">true to compress the message before sending; otherwise, false. The default is false.</param>
  public void PostMsg<T>(
    TClientGroup _clientGroup,
    TClientData _clientData,
    T _msg,
    bool _compress = false)
    where T : notnull
  {
    var buffer = CreateWsMessage(_msg, _compress);
    p_specificUserQueueSubj.OnNext(new SpecificUserTask(_clientGroup, _clientData, buffer, _compress));
  }

  /// <summary>Asynchronously sends a message to the specified WebSocket session.</summary>
  /// <remarks>This method does not catch exceptions caused by network error.</remarks>
  /// <param name="_session">The WebSocket session through which the message will be sent.</param>
  /// <param name="_asBinary">true to send the message as binary data; otherwise, false to send as text.</param>
  /// <param name="_data">The message payload to send, as a read-only memory buffer of bytes.</param>
  /// <param name="_ct">A cancellation token that can be used to cancel the send operation.</param>
  /// <returns>true if the message was sent successfully; otherwise, false if the session is not open.</returns>
  private async Task<bool> SendMsgUnsafeAsync(
    WebSocketSession<TClientData, TClientGroup> _session,
    bool _asBinary,
    ReadOnlyMemory<byte> _data,
    CancellationToken _ct)
  {
    if (_session.SocketState != WebSocketState.Open)
      return false;

    await _session.SendAsync(_data, _asBinary ? WebSocketMessageType.Binary : WebSocketMessageType.Text, true, _ct);
    return true;
  }

  private async Task CreateNewLoopAsync(
    WebSocketSession<TClientData, TClientGroup> _session,
    SemaphoreSlim _completeSignal)
  {
    p_sessions.TryAdd(_session.ConnectionId, _session);

    using var cts = new CancellationTokenSource(p_connectionMaxIdleTime);

    WebSocketReceiveResult? receiveResult = null;

    var buffer = ArrayPool<byte>.Shared.Rent(100 * 1024);

    try
    {
      p_clientConnectedFlow.OnNext(_session);

      receiveResult = await _session.ReceiveAsync(buffer, cts.Token);

      while (!receiveResult.CloseStatus.HasValue && !cts.IsCancellationRequested)
      {
        cts.CancelAfter(p_connectionMaxIdleTime);

        if (receiveResult.EndOfMessage == false)
        {
          p_log.Error($"Received msg that is too big for buffer (session: '{_session.ConnectionId}'). Closing session.");
          break;
        }

        try
        {
          if (TryParseWsMsg(buffer.AsSpan()[..receiveResult.Count], out var msg, out _))
            p_incomingMsgs.OnNext(new WsMsg(_session.ConnectionId, _session.ClientData, _session.ClientGroup, msg));
        }
        finally
        {
          receiveResult = await _session.ReceiveAsync(buffer, cts.Token);
        }
      }
    }
    catch (OperationCanceledException)
    {
      // don't care
    }
    catch (WebSocketException wsEx) when (wsEx.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
    {
      // don't care
    }
    catch (Exception ex)
    {
      p_log.Error($"Error occured in message loop of session '{_session.ConnectionId}': {ex}");
    }
    finally
    {
      ArrayPool<byte>.Shared.Return(buffer, false);
      p_sessions.TryRemove(_session.ConnectionId, out _);
      p_clientDisconnectedFlow.OnNext(_session);
    }

    try
    {
      if (_session.SocketState == WebSocketState.Open)
      {
        using var closeCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        if (receiveResult is not null)
          await _session.CloseAsync(receiveResult.CloseStatus ?? WebSocketCloseStatus.NormalClosure, receiveResult.CloseStatusDescription, closeCts.Token);
        else
          await _session.CloseAsync(WebSocketCloseStatus.NormalClosure, $"Closed normally (session: '{_session.ConnectionId}')", closeCts.Token);
      }
    }
    catch (Exception ex)
    {
      p_log.Error($"Error occured while closing websocket session '{_session.ConnectionId}': {ex}");
    }

    _completeSignal.Release();
  }

  private byte[] CreateWsMessage<T>(T _msg, bool _gzipped = false) where T : notnull
  {
    var type = typeof(T);
    if (!p_msgTypesReverse.TryGetValue(type, out var typeSlug))
      throw new InvalidOperationException($"Unknown type '{type}'");

    var baseMsg = new WsBaseMsg(typeSlug, _msg);
    var bytes = JsonSerializer.SerializeToUtf8Bytes(baseMsg, typeof(WsBaseMsg), p_jsonCtx);

    if (!_gzipped)
      return bytes;

    byte[] gzippedBytes;
    using (var sourceStream = new MemoryStream(bytes, false))
    using (var outputStream = new MemoryStream())
    {
      using (var compression = new GZipStream(outputStream, CompressionMode.Compress, leaveOpen: true))
        sourceStream.CopyTo(compression);

      gzippedBytes = outputStream.ToArray();
    }

    return gzippedBytes;
  }

  private bool TryParseWsMsg(
    ReadOnlySpan<byte> _msg,
    [NotNullWhen(true)] out object? _payload,
    [NotNullWhen(true)] out Type? _payloadType)
  {
    _payload = null;
    _payloadType = null;
    try
    {
      if (JsonSerializer.Deserialize(_msg, typeof(WsBaseMsg), p_jsonCtx) is not WsBaseMsg baseMsg)
        return false;

      if (!p_msgTypes.TryGetValue(baseMsg.Type, out var type))
        return false;

      _payload = ((JsonElement)baseMsg.Payload).Deserialize(type, p_jsonCtx);
      if (_payload == null)
        return false;

      _payloadType = type;
      return true;
    }
    catch
    {
      return false;
    }
  }

}

public static class WebSocketServer
{
  /// <summary>
  /// Creates and initializes a new WebSocket server instance with the specified configuration and logging callback.
  /// </summary>
  /// <remarks>The caller is responsible for disposing the returned IDisposable to ensure proper cleanup of
  /// server resources. The server instance is provided via the out parameter and is valid until the returned
  /// IDisposable is disposed.</remarks>
  /// <typeparam name="TClientData">The type used to identify individual clients. Must be non-null and support value equality.</typeparam>
  /// <typeparam name="TClientGroup">The type used to identify client groups. Must be non-null and support value equality.</typeparam>
  /// <param name="_onLog">A callback action that is invoked for each log entry generated by the server.</param>
  /// <param name="_jsonCtx">The JSON serializer context used for message serialization and deserialization. Must include serialization of type <see cref="WsBaseMsg"></param>
  /// <param name="_msgTypes">A read-only dictionary mapping message type names to their corresponding .NET types. Used to resolve message types during communication.</param>
  /// <param name="_connectionMaxIdleTime">The maximum duration a connection can remain idle before being closed.</param>
  /// <param name="_serverInstance">When this method returns, contains the initialized WebSocket server instance.</param>
  /// <returns>An IDisposable that controls the lifetime of the server. Disposing the returned object will shut down the server and release associated resources.</returns>
  public static IDisposable Create<TClientData, TClientGroup>(
    Action<LogEntry> _onLog,
    JsonSerializerContext _jsonCtx,
    IReadOnlyDictionary<string, Type> _msgTypes,
    TimeSpan _connectionMaxIdleTime,
    out WebSocketServer<TClientData, TClientGroup> _serverInstance)
    where TClientData : notnull, IEquatable<TClientData>
    where TClientGroup : notnull, IEquatable<TClientGroup>
  {
    var lifetime = new Lifetime();
    var log = new GenericLog();

    log.LogEntries
      .Subscribe(_ => _onLog(_), lifetime);

    _serverInstance = new WebSocketServer<TClientData, TClientGroup>(lifetime, log, _jsonCtx, _msgTypes, _connectionMaxIdleTime);
    return lifetime;
  }
}