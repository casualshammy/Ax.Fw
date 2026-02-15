using Ax.Fw.Extensions;
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
using System.Text;
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
/// <typeparam name="TSessionId">The type used to uniquely identify WebSocket sessions.</typeparam>
/// <typeparam name="TSessionGroup">The type used to group WebSocket sessions.</typeparam>
public class WebSocketServer<TSessionId, TSessionGroup>
  where TSessionGroup : notnull, IEquatable<TSessionGroup>
  where TSessionId : notnull, IEquatable<TSessionId>
{
  private sealed record WsMsg(TSessionId Sender, TSessionGroup SessionGroup, object Msg);
  private sealed record BroadcastTask(TSessionGroup SessionGroup, byte[] Data, bool Compressed);

  private readonly Subject<WsMsg> p_incomingMsgs = new();
  private readonly Subject<WebSocketSession<TSessionId, TSessionGroup>> p_clientConnectedFlow = new();
  private readonly Subject<WebSocketSession<TSessionId, TSessionGroup>> p_clientDisconnectedFlow = new();
  private readonly Subject<BroadcastTask> p_broadcastQueueSubj = new();
  private readonly IReadOnlyLifetime p_lifetime;
  private readonly JsonSerializerContext p_jsonCtx;
  private readonly IReadOnlyDictionary<string, Type> p_msgTypes;
  private readonly IReadOnlyDictionary<Type, string> p_msgTypesReverse;
  private readonly int p_connectionMaxIdleTimeMs;
  private readonly Action<string>? p_onError;
  private readonly ConcurrentDictionary<long, WebSocketSession<TSessionId, TSessionGroup>> p_sessions = new();
  private long p_sessionsCount = 0;

  /// <summary>
  /// Initializes a new instance of the WebSocketServer class, configuring message serialization, supported message
  /// types, connection idle timeout, and error handling.
  /// </summary>
  /// <remarks>The server uses the provided lifetime to manage its resources and ensure proper cleanup. The
  /// error callback allows custom handling of operational errors, such as failures during message broadcasting.
  /// Supported message types must be registered in the msgTypes dictionary; unregistered types will not be
  /// processed.</remarks>
  /// <param name="_lifetime">The lifetime scope that controls the disposal and shutdown of the server and its resources. The server will be
  /// disposed when this lifetime ends.</param>
  /// <param name="_jsonCtx">The JSON serializer context used for serializing and deserializing messages. Must be of type
  /// <see cref="WebSocketServerJsonCtx"/>.</param>
  /// <param name="_msgTypes">A read-only dictionary mapping message type names to their corresponding .NET types. Defines the set of supported
  /// message types for the server.</param>
  /// <param name="_connectionMaxIdleTimeMs">The maximum duration, in milliseconds, that a connection can remain idle before being closed. Defaults to one hour
  /// if not specified.</param>
  /// <param name="_onError">An optional callback invoked with an error message when an error occurs during server operations, such as
  /// broadcasting messages.</param>
  /// <exception cref="ArgumentException">Thrown if the provided JSON serializer context is not of type WebSocketServerJsonCtx.</exception>
  public WebSocketServer(
    IReadOnlyLifetime _lifetime,
    JsonSerializerContext _jsonCtx,
    IReadOnlyDictionary<string, Type> _msgTypes,
    int _connectionMaxIdleTimeMs = 60 * 60 * 1000,
    Action<string>? _onError = null)
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

    p_connectionMaxIdleTimeMs = _connectionMaxIdleTimeMs;
    p_onError = _onError;

    var broadcastScheduler = _lifetime.ToDisposeOnEnded(new EventLoopScheduler());
    p_broadcastQueueSubj
      .ObserveOn(broadcastScheduler)
      .SelectAsync(async (_msg, _ct) =>
      {
        try
        {
          await BroadcastMsgAsync(_msg.SessionGroup, _msg.Data, _msg.Compressed, _ct);
        }
        catch (Exception ex)
        {
          _onError?.Invoke($"Can't broadcast msg from post queue: {ex}");
        }
      }, broadcastScheduler)
      .Subscribe(_lifetime);
  }

  /// <summary>
  /// Gets an observable sequence that signals when a new client establishes a WebSocket connection.
  /// </summary>
  public IObservable<WebSocketSession<TSessionId, TSessionGroup>> ClientConnected => p_clientConnectedFlow;

  /// <summary>
  /// Gets an observable sequence that signals when a client disconnects from a WebSocket session.
  /// </summary>
  public IObservable<WebSocketSession<TSessionId, TSessionGroup>> ClientDisconnected => p_clientDisconnectedFlow;

  /// <summary>
  /// Gets a read-only list of all active WebSocket sessions.
  /// </summary>
  public IReadOnlyList<WebSocketSession<TSessionId, TSessionGroup>> Sessions => [.. p_sessions.Values];

  /// <summary>
  /// Accepts a WebSocket connection and initializes a new session asynchronously.
  /// </summary>
  /// <remarks>If the provided WebSocket is not open, the method returns <see langword="false"/> immediately.
  /// Otherwise, it creates a new session and waits for session to end.</remarks>
  /// <param name="_id">The unique identifier for the session to be created.</param>
  /// <param name="_sessionGroup">The session group associated with the new WebSocket session.</param>
  /// <param name="_webSocket">The WebSocket instance to be accepted. Must be in the <see cref="WebSocketState.Open"/> state.</param>
  public async Task<bool> AcceptSocketAsync(
    TSessionId _id,
    TSessionGroup _sessionGroup,
    WebSocket _webSocket)
  {
    if (_webSocket.State != WebSocketState.Open)
      return false;

    var session = new WebSocketSession<TSessionId, TSessionGroup>(_sessionGroup, _id, _webSocket);
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
      p_onError?.Invoke($"Waiting for loop is failed: {ex}");
    }

    return true;
  }

  /// <summary>
  /// Returns an observable sequence of incoming messages of the specified type that belong to the given session group.
  /// </summary>
  /// <remarks>The returned sequence excludes messages that do not match the specified type or session group.</remarks>
  /// <typeparam name="T">The message type to filter for. Must be a reference type.</typeparam>
  /// <param name="_group">The session group to filter incoming messages by. Only messages associated with this group are included in the
  /// sequence.</param>
  /// <returns>An observable sequence containing messages of type T from the specified session group. The sequence emits only
  /// messages that match the type and group criteria.</returns>
  public IObservable<T> IncomingMessagesOfType<T>(TSessionGroup _group)
    where T : class
  {
    return p_incomingMsgs
      .Where(_ => _.SessionGroup.Equals(_group))
      .Select(_ =>
      {
        if (_.Msg.GetType() is T typedMsg)
          return typedMsg;
        else
          return null;
      })
      .WhereNotNull();
  }

  /// <summary>
  /// Broadcasts a message to all sessions in the specified session group asynchronously.
  /// </summary>
  /// <typeparam name="T">The type of the message to broadcast. Must not be null.</typeparam>
  /// <param name="_sessionGroup">The session group to which the message will be broadcast.</param>
  /// <param name="_msg">The message to broadcast to all sessions in the group. Cannot be null.</param>
  /// <param name="_compress">A value indicating whether the message should be compressed before broadcasting. If <see langword="true"/>, the
  /// message will be compressed.</param>
  /// <param name="_ct">A cancellation token that can be used to cancel the broadcast operation.</param>
  /// <returns>A task that represents the asynchronous operation. The task result contains the number of sessions to which the
  /// message was successfully broadcast.</returns>
  public async Task<int> BroadcastMsgAsync<T>(
    TSessionGroup _sessionGroup,
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
  /// <param name="_sessionGroup">The session group to which the message will be broadcast.</param>
  /// <param name="_msg">The message data to send to each session, as a byte array.</param>
  /// <param name="_asBinary">Indicates whether the message should be sent as binary (<see langword="true"/>) or as text (<see
  /// langword="false"/>).</param>
  /// <param name="_ct">A cancellation token that can be used to cancel the broadcast operation.</param>
  /// <returns>The number of sessions in the group that successfully received the message.</returns>
  public async Task<int> BroadcastMsgAsync(
    TSessionGroup _sessionGroup,
    byte[] _msg,
    bool _asBinary,
    CancellationToken _ct)
  {
    var totalSent = 0;

    await Parallel.ForEachAsync(p_sessions.Where(_ => _.Value.SessionGroup.Equals(_sessionGroup)), _ct, async (_pair, _c) =>
    {
      var (connectionIndex, session) = _pair;
      try
      {
        if (session.Socket.State == WebSocketState.Open)
        {
          await session.Socket.SendAsync(_msg, _asBinary ? WebSocketMessageType.Binary : WebSocketMessageType.Text, true, _ct);
          ++totalSent;
        }
      }
      catch (Exception ex)
      {
        p_onError?.Invoke($"Can't send msg to socket '#{connectionIndex} / {session.Id}': {ex}");
      }
    });

    return totalSent;
  }

  /// <summary>
  /// Posts a broadcast message to all sessions in the specified session group.
  /// </summary>
  /// <remarks>This method enqueues the broadcast message for asynchronous delivery to all sessions in the group.</remarks>
  /// <typeparam name="T">The type of the message to broadcast.</typeparam>
  /// <param name="_sessionGroup">The session group to which the broadcast message will be sent.</param>
  /// <param name="_msg">The message to broadcast to all sessions in the group.</param>
  /// <param name="_compress">Indicates whether the message should be compressed before broadcasting. Set to <see langword="true"/> to enable
  /// compression; otherwise, <see langword="false"/>.</param>
  public void PostBroadcastMsg<T>(
    TSessionGroup _sessionGroup,
    T _msg,
    bool _compress = false) where T : notnull
  {
    var buffer = CreateWsMessage(_msg, _compress);
    p_broadcastQueueSubj.OnNext(new BroadcastTask(_sessionGroup, buffer, _compress));
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
    WebSocketSession<TSessionId, TSessionGroup> _session,
    T _msg,
    bool _compress,
    CancellationToken _ct) where T : notnull
  {
    var buffer = CreateWsMessage(_msg, _compress);

    try
    {
      if (_session.Socket.State == WebSocketState.Open)
        await _session.Socket.SendAsync(buffer, _compress ? WebSocketMessageType.Binary : WebSocketMessageType.Text, true, _ct);
    }
    catch (Exception ex)
    {
      p_onError?.Invoke($"Can't send msg to socket: {ex}");
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
  public async Task SendMsgAsync<T>(
    IEnumerable<WebSocketSession<TSessionId, TSessionGroup>> _sessions,
    T _msg,
    bool _compress,
    CancellationToken _ct) where T : notnull
  {
    var buffer = CreateWsMessage(_msg, _compress);

    foreach (var session in _sessions)
    {
      try
      {
        if (session.Socket.State == WebSocketState.Open)
          await session.Socket.SendAsync(buffer, _compress ? WebSocketMessageType.Binary : WebSocketMessageType.Text, true, _ct);
      }
      catch (Exception ex)
      {
        p_onError?.Invoke($"Can't send msg to socket: {ex}");
      }
    }
  }

  private async Task CreateNewLoopAsync(
    WebSocketSession<TSessionId, TSessionGroup> _session,
    SemaphoreSlim _completeSignal)
  {
    var session = _session;
    var sessionIndex = Interlocked.Increment(ref p_sessionsCount);
    p_sessions.TryAdd(sessionIndex, session);

    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(p_connectionMaxIdleTimeMs));

    WebSocketReceiveResult? receiveResult = null;

    var buffer = ArrayPool<byte>.Shared.Rent(100 * 1024);

    try
    {
      p_clientConnectedFlow.OnNext(session);

      receiveResult = await session.Socket.ReceiveAsync(buffer, cts.Token);

      while (!receiveResult.CloseStatus.HasValue && !cts.IsCancellationRequested)
      {
        cts.CancelAfter(TimeSpan.FromMilliseconds(p_connectionMaxIdleTimeMs));

        try
        {
          if (TryParseWsMsg(buffer[..receiveResult.Count], out var msg, out var msgType))
            p_incomingMsgs.OnNext(new WsMsg(_session.Id, session.SessionGroup, msg));
        }
        finally
        {
          receiveResult = await session.Socket.ReceiveAsync(buffer, cts.Token);
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
      p_onError?.Invoke($"Error occured in loop: {ex}");
    }
    finally
    {
      ArrayPool<byte>.Shared.Return(buffer, false);
      p_sessions.TryRemove(sessionIndex, out _);
      p_clientDisconnectedFlow.OnNext(session);
    }

    try
    {
      if (session.Socket.State == WebSocketState.Open)
      {
        if (receiveResult is not null)
          await session.Socket.CloseAsync(receiveResult.CloseStatus ?? WebSocketCloseStatus.NormalClosure, receiveResult.CloseStatusDescription, CancellationToken.None);
        else
          await session.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, $"Closed normally (session: '{sessionIndex}')", CancellationToken.None);
      }
    }
    catch (Exception ex)
    {
      p_onError?.Invoke($"Error occured while closing websocket: {ex}");
    }

    _completeSignal.Release();
  }

  private byte[] CreateWsMessage<T>(T _msg, bool _gzipped = false) where T : notnull
  {
    var type = typeof(T);
    if (!p_msgTypesReverse.TryGetValue(type, out var typeSlug))
      throw new InvalidOperationException($"Unknown type '{type}'");

    var baseMsg = new WsBaseMsg(typeSlug, _msg);
    var json = JsonSerializer.Serialize(baseMsg, typeof(WsBaseMsg), p_jsonCtx);
    var bytes = Encoding.UTF8.GetBytes(json);

    if (!_gzipped)
      return bytes;

    byte[] gzippedBytes;
    using (var sourceStream = new MemoryStream(bytes))
    using (var outputStream = new MemoryStream())
    {
      using (var compression = new GZipStream(outputStream, CompressionMode.Compress, leaveOpen: true))
        sourceStream.CopyTo(compression);

      gzippedBytes = outputStream.ToArray();
    }

    return gzippedBytes;
  }

  private bool TryParseWsMsg(
    byte[] _msg,
    [NotNullWhen(true)] out object? _payload,
    [NotNullWhen(true)] out Type? _payloadType)
  {
    _payload = null;
    _payloadType = null;
    try
    {
      var json = Encoding.UTF8.GetString(_msg);
      if (JsonSerializer.Deserialize(json, typeof(WsBaseMsg), p_jsonCtx) is not WsBaseMsg baseMsg)
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
