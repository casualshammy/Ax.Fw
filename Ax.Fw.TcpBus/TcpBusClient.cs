#nullable enable
using Ax.Fw.Bus;
using Ax.Fw.Crypto;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Attributes;
using Ax.Fw.SharedTypes.Data.Bus;
using Ax.Fw.SharedTypes.Data.Workers;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Workers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WatsonTcp;

namespace Ax.Fw.TcpBus;

public static class TcpBusClientFactory
{
  public static IDisposable Create(
      int _port,
      out TcpBusClient _serverInstance)
  {
    var lifetime = new Lifetime();

    _serverInstance = new TcpBusClient(lifetime, _port);

    return Disposable.Create(lifetime.End);
  }

  public static IDisposable Create(
      int _port,
      string _host,
      out TcpBusClient _serverInstance)
  {
    var lifetime = new Lifetime();

    _serverInstance = new TcpBusClient(lifetime, _port, _host);

    return Disposable.Create(lifetime.End);
  }

  public static IDisposable Create(
      int _port,
      IScheduler _scheduler,
      out TcpBusClient _serverInstance)
  {
    var lifetime = new Lifetime();

    _serverInstance = new TcpBusClient(lifetime, _scheduler, _port);

    return Disposable.Create(lifetime.End);
  }

  public static IDisposable Create(
      int _port,
      string _host,
      IScheduler _scheduler,
      out TcpBusClient _serverInstance)
  {
    var lifetime = new Lifetime();

    _serverInstance = new TcpBusClient(lifetime, _scheduler, _port, _host);

    return Disposable.Create(lifetime.End);
  }

}

public class TcpBusClient : ITcpBusClient
{
  private readonly WatsonTcpClient p_client;
  private readonly Subject<TcpMsg> p_msgFlow = new();
  private readonly ConcurrentDictionary<Type, TcpMsg> p_lastMsg = new();
  private readonly IReadOnlyLifetime p_lifetime;
  private readonly IScheduler p_scheduler;
  private readonly byte[]? p_password;
  private readonly IReadOnlyDictionary<string, Type> p_typesCache;
  private readonly IReadOnlyDictionary<Type, string> p_typesCacheReverse;
  private readonly Subject<byte[]> p_failedTcpMsgFlow = new();
  private readonly Subject<byte[]> p_incomingMsgFlow = new();

  public TcpBusClient(IReadOnlyLifetime _lifetime, IScheduler _scheduler, int _port, string _host = "127.0.0.1", string? _password = null)
  {
    var typesCache = new Dictionary<string, Type>();
    var typesCacheReverse = new Dictionary<Type, string>();
    foreach (var type in Utilities.GetTypesWithAttr<TcpMsgTypeAttribute>(true))
    {
      var attr = Utilities.GetAttribute<TcpMsgTypeAttribute>(type);
      if (attr != null)
      {
        typesCache.Add(attr.TypeSlug, type);
        typesCacheReverse.Add(type, attr.TypeSlug);
      }
    }

    p_typesCache = typesCache;
    p_typesCacheReverse = typesCacheReverse;
    p_lifetime = _lifetime;
    p_scheduler = _scheduler;
    p_password = _password != null ? Encoding.UTF8.GetBytes(_password) : null;
    _lifetime.ToDisposeOnEnded(p_msgFlow);
    _lifetime.ToDisposeOnEnded(p_incomingMsgFlow);

    p_client = _lifetime.ToDisposeOnEnded(new WatsonTcpClient(_host, _port))!;
    p_client.Events.MessageReceived += MessageReceivedRaw;
    _lifetime.DoOnEnding(() => p_client.Events.MessageReceived -= MessageReceivedRaw);

    async Task<bool> sendTcpMsgJob(JobContext<byte[], Unit> _ctx)
    {
      try
      {
        if (!p_client.Connected)
          p_client.Connect();

        return await p_client.SendAsync(_ctx.JobInfo.Job, token: _ctx.CancellationToken);
      }
      catch
      {
        return false;
      }
    }
    Task<PenaltyInfo> sendTcpMsgJobPenalty(JobFailContext<byte[]> _ctx)
    {
      return Task.FromResult(new PenaltyInfo(_ctx.FailedCounter < 100, TimeSpan.FromMilliseconds(_ctx.FailedCounter * 300))); // 300ms - 30 sec
    }

    WorkerTeam.Run(p_failedTcpMsgFlow, sendTcpMsgJob, sendTcpMsgJobPenalty, _lifetime, 4);

    p_incomingMsgFlow
        .ObserveOn(p_scheduler)
        .SelectAsync(MessageReceivedAsync, p_scheduler)
        .Subscribe(_lifetime);

    Observable
        .Interval(TimeSpan.FromSeconds(5))
        .Subscribe(_ =>
        {
          try
          {
            if (!p_client.Connected)
              p_client.Connect();
          }
          catch { }
        }, _lifetime);

    if (!p_client.Connected)
      p_client.Connect();
  }

  public TcpBusClient(IReadOnlyLifetime _lifetime, int _port, string _host = "127.0.0.1", string? _password = null) : this(_lifetime, ThreadPoolScheduler.Instance, _port, _host, _password)
  { }

  public bool Connected => p_client.Connected;

  private void MessageReceivedRaw(object? _sender, MessageReceivedEventArgs _args) => p_incomingMsgFlow.OnNext(_args.Data);

  private async Task MessageReceivedAsync(byte[] _msgData, CancellationToken _ct)
  {
    byte[]? bytes;
    if (p_password != null)
      bytes = await AesCbc.DecryptAsync(_msgData, p_password, true, _ct);
    else
      bytes = _msgData;

    try
    {
      var msg = JsonSerializer.Deserialize(bytes, typeof(TcpMsg)) as TcpMsg;

      if (msg == null)
        return;
      if (!p_typesCache.TryGetValue(msg.TypeSlug, out var type))
        return;

      p_lastMsg[type] = msg;
      p_msgFlow.OnNext(msg);
    }
    catch
    {
      return;
    }
  }

  /// <summary>
  /// Get Observable of messages by type
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="_includeLastValue"></param>
  /// <returns></returns>
  public IObservable<T> OfType<T>(bool _includeLastValue = false) where T : notnull
  {
    if (!p_typesCacheReverse.TryGetValue(typeof(T), out var typeSlug))
      throw new ArgumentNullException(nameof(T), $"This type cannot be deserialized!");

    if (_includeLastValue && p_lastMsg.TryGetValue(typeof(T), out var lastMsg))
      return p_msgFlow
        .Merge(Observable.Return(lastMsg))
        .Where(_ => _.TypeSlug == typeSlug)
        .Select(_ => JsonSerializer.Deserialize<T>(_.Data))
        .WhereNotNull()
        .ObserveOn(p_scheduler);
    else
      return p_msgFlow
        .Where(_ => _.TypeSlug == typeSlug)
        .Select(_ => JsonSerializer.Deserialize<T>(_.Data))
        .WhereNotNull()
        .ObserveOn(p_scheduler);
  }

  /// <summary>
  /// Get Observable of messages by type
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="_includeLastValue"></param>
  /// <returns></returns>
  public IObservable<TcpMsg> OfTypeRaw<T>(bool _includeLastValue = false) where T : notnull
  {
    if (!p_typesCacheReverse.TryGetValue(typeof(T), out var typeSlug))
      throw new ArgumentNullException(nameof(T), $"This type cannot be deserialized!");

    if (_includeLastValue && p_lastMsg.TryGetValue(typeof(T), out var lastMsg))
      return p_msgFlow
        .Merge(Observable.Return(lastMsg))
        .Where(_ => _.TypeSlug == typeSlug)
        .WhereNotNull()
        .ObserveOn(p_scheduler);
    else
      return p_msgFlow
        .Where(_ => _.TypeSlug == typeSlug)
        .WhereNotNull()
        .ObserveOn(p_scheduler);
  }

  /// <summary>
  /// Send message to bus
  /// </summary>
  /// <param name="_data"></param>
  public async Task PostMsgAsync<T>(T _data, CancellationToken _ct) where T : notnull
  {
    if (_data == null)
      throw new ArgumentNullException(nameof(_data));
    if (!p_typesCacheReverse.TryGetValue(typeof(T), out var typeSlug))
      throw new ArgumentNullException(nameof(T), $"This type cannot be serialized!");

    var dataJson = JsonSerializer.Serialize(_data);

    var tcpMsg = new TcpMsg(
      Guid.NewGuid(),
      typeSlug,
      Encoding.UTF8.GetBytes(dataJson));

    await PostMsgAsync(tcpMsg, _ct);
  }

  public async Task PostMsgAsync(TcpMsg _tcpMsg, CancellationToken _ct)
  {
    p_lastMsg[_tcpMsg.Data.GetType()] = _tcpMsg;
    p_msgFlow.OnNext(_tcpMsg);

    var tcpMsgBytes = JsonSerializer.SerializeToUtf8Bytes(_tcpMsg);
    if (p_password != null)
      tcpMsgBytes = await AesCbc.EncryptAsync(tcpMsgBytes, p_password, true, _ct);

    if (!await p_client.SendAsync(tcpMsgBytes, token: _ct))
      p_failedTcpMsgFlow.OnNext(tcpMsgBytes);
  }

  public async Task<TRes?> PostReqResOrDefaultAsync<TReq, TRes>(TReq _req, TimeSpan _timeout, CancellationToken _ct)
      where TReq : notnull
      where TRes : notnull
  {
    if (!p_typesCacheReverse.TryGetValue(typeof(TReq), out var reqTypeSlug))
      throw new ArgumentNullException(nameof(TReq), $"This type cannot be serialized!");
    if (!p_typesCacheReverse.TryGetValue(typeof(TRes), out var resTypeSlug))
      throw new ArgumentNullException(nameof(TRes), $"This type cannot be deserialized!");

    var guid = Guid.NewGuid();
    var ignoredGuid = Guid.NewGuid();

    try
    {
      var value = await Observable
          .Merge(
              p_msgFlow.ObserveOn(p_scheduler).Where(_x => _x.Guid == guid && _x.TypeSlug == resTypeSlug),
              Observable.Timer(_timeout, p_scheduler).Select(_ => new TcpMsg(Guid.Empty, "", Array.Empty<byte>())),
              Observable.Return(Unit.Default).SelectAsync(async (_, _ct) =>
              {
                await PostMsgAsync(new TcpMsg(guid, reqTypeSlug, JsonSerializer.SerializeToUtf8Bytes(_req)), _ct);
                return new TcpMsg(ignoredGuid, "", Array.Empty<byte>());
              }))
          .FirstOrDefaultAsync(_x => _x.Guid != ignoredGuid)
          .ToTask(_ct);

      if (value != default && value.Guid != Guid.Empty)
        return JsonSerializer.Deserialize<TRes>(value.Data);

      return default;
    }
    catch (TaskCanceledException)
    {
      throw new OperationCanceledException(_ct);
    }
  }

  /// <summary>
  /// Create a handler of messages of specific type ('server')
  /// </summary>
  /// <typeparam name="TReq"></typeparam>
  /// <typeparam name="TRes"></typeparam>
  /// <param name="_func"></param>
  /// <returns></returns>
  public IDisposable OfReqRes<TReq, TRes>(Func<TReq, TRes> _func)
      where TReq : notnull
      where TRes : notnull
  {
    if (!p_typesCacheReverse.TryGetValue(typeof(TReq), out var reqTypeSlug))
      throw new ArgumentNullException(nameof(TReq), $"This type cannot be serialized!");
    if (!p_typesCacheReverse.TryGetValue(typeof(TRes), out var resTypeSlug))
      throw new ArgumentNullException(nameof(TRes), $"This type cannot be deserialized!");

    return p_msgFlow
        .Where(_ => _.TypeSlug == reqTypeSlug)
        .ObserveOn(p_scheduler)
        .SelectAsync(async (_tcpMsg, _ct) =>
        {
          var guid = _tcpMsg.Guid;
          var msg = JsonSerializer.Deserialize<TReq>(_tcpMsg.Data);
          if (msg == null)
            return;

          var result = _func(msg);
          await PostMsgAsync(new TcpMsg(guid, resTypeSlug, JsonSerializer.SerializeToUtf8Bytes(result)), _ct);
        }, p_scheduler)
        .Subscribe();
  }

  /// <summary>
  /// Create a handler of messages of specific type ('server')
  /// </summary>
  /// <typeparam name="TReq"></typeparam>
  /// <typeparam name="TRes"></typeparam>
  /// <param name="_func"></param>
  /// <returns></returns>
  public IDisposable OfReqRes<TReq, TRes>(Func<TReq, Task<TRes>> _func)
      where TReq : notnull
      where TRes : notnull
  {
    if (!p_typesCacheReverse.TryGetValue(typeof(TReq), out var reqTypeSlug))
      throw new ArgumentNullException(nameof(TReq), $"This type cannot be serialized!");
    if (!p_typesCacheReverse.TryGetValue(typeof(TRes), out var resTypeSlug))
      throw new ArgumentNullException(nameof(TRes), $"This type cannot be deserialized!");

    return p_msgFlow
        .Where(_ => _.TypeSlug == reqTypeSlug)
        .ObserveOn(p_scheduler)
        .SelectAsync(async (_tcpMsg, _ct) =>
        {
          var guid = _tcpMsg.Guid;
          var msg = JsonSerializer.Deserialize<TReq>(_tcpMsg.Data);
          if (msg == null)
            return;

          var result = await _func(msg);
          await PostMsgAsync(new TcpMsg(guid, resTypeSlug, JsonSerializer.SerializeToUtf8Bytes(result)), _ct);
        }, p_scheduler)
        .Subscribe();
  }

  /// <summary>
  /// Create a handler of messages of specific type ('server')
  /// </summary>
  /// <typeparam name="TReq"></typeparam>
  /// <typeparam name="TRes"></typeparam>
  /// <param name="_func"></param>
  /// <param name="_lifetime"></param>
  public void OfReqRes<TReq, TRes>(Func<TReq, TRes> _func, ILifetime _lifetime)
      where TReq : notnull
      where TRes : notnull
  {
    if (!p_typesCacheReverse.TryGetValue(typeof(TReq), out var reqTypeSlug))
      throw new ArgumentNullException(nameof(TReq), $"This type cannot be serialized!");
    if (!p_typesCacheReverse.TryGetValue(typeof(TRes), out var resTypeSlug))
      throw new ArgumentNullException(nameof(TRes), $"This type cannot be deserialized!");

    p_msgFlow
      .Where(_ => _.TypeSlug == reqTypeSlug)
      .ObserveOn(p_scheduler)
      .SelectAsync(async (_tcpMsg, _ct) =>
      {
        var guid = _tcpMsg.Guid;
        var msg = JsonSerializer.Deserialize<TReq>(_tcpMsg.Data);
        if (msg == null)
          return;

        var result = _func(msg);
        await PostMsgAsync(new TcpMsg(guid, resTypeSlug, JsonSerializer.SerializeToUtf8Bytes(result)), _ct);
      }, p_scheduler)
      .Subscribe(_lifetime);
  }

  /// <summary>
  /// Create a handler of messages of specific type ('server')
  /// </summary>
  /// <typeparam name="TReq"></typeparam>
  /// <typeparam name="TRes"></typeparam>
  /// <param name="_func"></param>
  /// <param name="_lifetime"></param>
  public void OfReqRes<TReq, TRes>(Func<TReq, Task<TRes>> _func, ILifetime _lifetime)
      where TReq : notnull
      where TRes : notnull
  {
    if (!p_typesCacheReverse.TryGetValue(typeof(TReq), out var reqTypeSlug))
      throw new ArgumentNullException(nameof(TReq), $"This type cannot be serialized!");
    if (!p_typesCacheReverse.TryGetValue(typeof(TRes), out var resTypeSlug))
      throw new ArgumentNullException(nameof(TRes), $"This type cannot be deserialized!");

    p_msgFlow
      .Where(_ => _.TypeSlug == reqTypeSlug)
      .ObserveOn(p_scheduler)
      .SelectAsync(async (_tcpMsg, _ct) =>
      {
        var guid = _tcpMsg.Guid;
        var msg = JsonSerializer.Deserialize<TReq>(_tcpMsg.Data);
        if (msg == null)
          return;

        var result = await _func(msg);
        await PostMsgAsync(new TcpMsg(guid, resTypeSlug, JsonSerializer.SerializeToUtf8Bytes(result)), _ct);
      }, p_scheduler)
      .Subscribe(_lifetime);
  }

}
