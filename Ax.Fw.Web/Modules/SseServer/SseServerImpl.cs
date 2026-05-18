using Ax.Fw.Collections;
using Ax.Fw.Extensions;
using Ax.Fw.Log;
using Ax.Fw.SharedTypes.Data.Log;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Web.Data.SseServer;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ax.Fw.Web.Modules.SseServer;

/// <summary>
/// Manages Server-Sent Events (SSE) sessions, allowing messages to be sent to individual clients
/// or broadcast to groups of clients. Supports keep-alive messaging and session lifecycle notifications.
/// </summary>
/// <typeparam name="TClientData">Application-defined data type associated with each connected client.</typeparam>
/// <typeparam name="TClientGroup">Type used to group clients for targeted broadcasts.</typeparam>
public class SseServerImpl<TClientData, TClientGroup>
  where TClientData : notnull, IEquatable<TClientData>
  where TClientGroup : notnull, IEquatable<TClientGroup>
{
  private sealed record BroadcastTask(TClientGroup ClientGroup, SseBaseMsg Msg);

  private readonly ILog p_log;
  private readonly IReadOnlyBijection<string, Type> p_msgTypesLut;
  private readonly JsonSerializerContext p_jsonCtx;
  private readonly int p_maxMsgInQueuePerClient;
  private readonly Subject<SseSession<TClientData, TClientGroup>> p_clientConnectedFlow = new();
  private readonly Subject<SseSession<TClientData, TClientGroup>> p_clientDisconnectedFlow = new();
  private readonly Subject<BroadcastTask> p_broadcastQueueSubj = new();
  private readonly ConcurrentDictionary<Guid, SseSession<TClientData, TClientGroup>> p_sessions = new();

  public SseServerImpl(
    IReadOnlyLifetime _lifetime,
    ILog _log,
    JsonSerializerContext _jsonCtx,
    IReadOnlyDictionary<string, Type> _msgTypes,
    TimeSpan _aliveMsgInterval,
    int _maxMsgInQueuePerClient)
  {
    p_jsonCtx = _jsonCtx;
    p_maxMsgInQueuePerClient = _maxMsgInQueuePerClient;
    p_log = _log;

    var msgTypesLut = new Bijection<string, Type>();
    p_msgTypesLut = msgTypesLut;
    foreach (var entry in _msgTypes)
      msgTypesLut.Set(entry.Key, entry.Value);

    var postScheduler = _lifetime.ToDisposeOnEnded(new EventLoopScheduler());
    p_broadcastQueueSubj
      .ObserveOn(postScheduler)
      .Subscribe(_task =>
      {
        try
        {
          BroadcastMsg(_task.ClientGroup, _task.Msg);
        }
        catch (Exception ex)
        {
          p_log.Error($"Can't broadcast msg from post queue: {ex}");
        }
      }, _lifetime);

    Observable
      .Interval(_aliveMsgInterval)
      .Subscribe(_ =>
      {
        var aliveMsg = new SseBaseMsg("alive", $"{{ \"index\": {_} }}");
        foreach (var session in p_sessions.Values)
          session.Write(aliveMsg);
      }, _lifetime);
  }

  /// <summary>
  /// Gets an observable sequence that signals when a new client establishes a connection.
  /// </summary>
  public IObservable<SseSession<TClientData, TClientGroup>> ClientConnected => p_clientConnectedFlow;

  /// <summary>
  /// Gets an observable sequence that signals when a client disconnects from a session.
  /// </summary>
  public IObservable<SseSession<TClientData, TClientGroup>> ClientDisconnected => p_clientDisconnectedFlow;

  /// <summary>
  /// Gets a read-only list of all active sessions.
  /// </summary>
  public ICollection<SseSession<TClientData, TClientGroup>> Sessions => p_sessions.Values;

  /// <summary>
  /// Registers a new client connection and returns a disposable that, when disposed, removes the session and signals disconnection.
  /// </summary>
  /// <param name="_clientData">Application-defined data associated with the client.</param>
  /// <param name="_clientGroup">The group the client belongs to, used for targeted broadcasts.</param>
  /// <param name="_session">The created <see cref="SseSession{TClientData,TClientGroup}"/> for this client.</param>
  /// <returns>An <see cref="IDisposable"/> that unregisters the session when disposed.</returns>
  public IDisposable AcceptClient(
    TClientData _clientData,
    TClientGroup _clientGroup,
    out SseSession<TClientData, TClientGroup> _session)
  {
    var sessionId = Guid.NewGuid();
    var session = new SseSession<TClientData, TClientGroup>(sessionId, _clientData, _clientGroup, p_maxMsgInQueuePerClient);
    p_sessions.TryAdd(sessionId, session);
    p_clientConnectedFlow.OnNext(session);

    _session = session;
    return Disposable.Create(() =>
    {
      if (p_sessions.Remove(sessionId, out var removedSession))
        p_clientDisconnectedFlow.OnNext(removedSession);
    });
  }

  /// <summary>
  /// Enqueues a message to be broadcast to all sessions in the specified client group.
  /// The message is serialized and delivered asynchronously.
  /// </summary>
  /// <typeparam name="T">The message type; must be registered in the message types lookup.</typeparam>
  /// <param name="_clientGroup">The target client group to broadcast the message to.</param>
  /// <param name="_msg">The message to broadcast.</param>
  public void PostBroadcastMsg<T>(
    TClientGroup _clientGroup,
    T _msg)
    where T : notnull
  {
    var msg = CreateMessage(_msg);
    p_broadcastQueueSubj.OnNext(new BroadcastTask(_clientGroup, msg));
  }

  /// <summary>
  /// Sends a message directly to a specific session.
  /// </summary>
  /// <typeparam name="T">The message type; must be registered in the message types lookup.</typeparam>
  /// <param name="_session">The target session to send the message to.</param>
  /// <param name="_msg">The message to send.</param>
  public void SendMsg<T>(
    SseSession<TClientData, TClientGroup> _session,
    T _msg)
    where T : notnull
  {
    var msg = CreateMessage(_msg);
    _session.Write(msg);
  }

  private int BroadcastMsg(
    TClientGroup _sessionGroup,
    SseBaseMsg _msg)
  {
    var totalSent = 0;
    var sessionsToSendEE = p_sessions.Values
      .Where(_ => _.ClientGroup.Equals(_sessionGroup));

    foreach (var session in sessionsToSendEE)
    {
      session.Write(_msg);
      ++totalSent;
    }

    return totalSent;
  }

  private SseBaseMsg CreateMessage<T>(T _msg)
    where T : notnull
  {
    var type = typeof(T);
    if (!p_msgTypesLut.TryGetByValue(type, out var typeSlug))
      throw new InvalidOperationException($"Unknown type '{type}'");

    var jsonData = JsonSerializer.Serialize(_msg, type, p_jsonCtx);
    return new SseBaseMsg(typeSlug, jsonData);
  }

}

public static class SseServerImpl
{
  /// <summary>
  /// Creates a new <see cref="SseServerImpl{TClientData,TClientGroup}"/>.
  /// </summary>
  /// <typeparam name="TClientData">Application-defined data type associated with each client.</typeparam>
  /// <typeparam name="TClientGroup">Type used to group clients for targeted broadcasts.</typeparam>
  /// <param name="_onLog">A callback invoked for each log entry produced by the server.</param>
  /// <param name="_jsonCtx">The <see cref="JsonSerializerContext"/> used for message serialization.</param>
  /// <param name="_msgTypes">A dictionary mapping type slugs to their corresponding .NET types.</param>
  /// <param name="_aliveMsgInterval">The interval at which keep-alive messages are sent to all connected clients.</param>
  /// <param name="_maxMsgInQueuePerClient">The maximum number of messages that can be queued per client.</param>
  /// <param name="_serverInstance">The created server instance.</param>
  /// <returns>An <see cref="IDisposable"/> that, when disposed, terminates the server lifetime and all associated resources.</returns>
  public static IDisposable Create<TClientData, TClientGroup>(
    Action<LogEntry> _onLog,
    JsonSerializerContext _jsonCtx,
    IReadOnlyDictionary<string, Type> _msgTypes,
    TimeSpan _aliveMsgInterval,
    int _maxMsgInQueuePerClient,
    out SseServerImpl<TClientData, TClientGroup> _serverInstance)
    where TClientData : notnull, IEquatable<TClientData>
    where TClientGroup : notnull, IEquatable<TClientGroup>
  {
    var lifetime = new Lifetime();
    var log = new GenericLog();

    log.LogEntries
      .Subscribe(_ => _onLog(_), lifetime);

    _serverInstance = new SseServerImpl<TClientData, TClientGroup>(lifetime, log, _jsonCtx, _msgTypes, _aliveMsgInterval, _maxMsgInQueuePerClient);
    return lifetime;
  }
}