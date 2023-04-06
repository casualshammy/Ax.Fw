#nullable enable
using Ax.Fw.Bus;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Data.Bus;
using Ax.Fw.SharedTypes.Data.Workers;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Workers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

    return Disposable.Create(lifetime.Complete);
  }

  public static IDisposable Create(
      int _port,
      string _host,
      out TcpBusClient _serverInstance)
  {
    var lifetime = new Lifetime();

    _serverInstance = new TcpBusClient(lifetime, _port, _host);

    return Disposable.Create(lifetime.Complete);
  }

  public static IDisposable Create(
      int _port,
      IScheduler _scheduler,
      out TcpBusClient _serverInstance)
  {
    var lifetime = new Lifetime();

    _serverInstance = new TcpBusClient(lifetime, _scheduler, _port);

    return Disposable.Create(lifetime.Complete);
  }

  public static IDisposable Create(
      int _port,
      string _host,
      IScheduler _scheduler,
      out TcpBusClient _serverInstance)
  {
    var lifetime = new Lifetime();

    _serverInstance = new TcpBusClient(lifetime, _scheduler, _port, _host);

    return Disposable.Create(lifetime.Complete);
  }

}

public class TcpBusClient : ITcpBusClient
{
  private readonly WatsonTcpClient p_client;
  private readonly Subject<BusMsgSerial> p_msgFlow = new();
  private readonly ConcurrentDictionary<Type, BusMsgSerial> p_lastMsg = new();
  private readonly IReadOnlyLifetime p_lifetime;
  private readonly IScheduler p_scheduler;
  private readonly byte[]? p_password;
  private readonly IReadOnlyDictionary<string, Type> p_typesCache;
  private readonly Subject<TcpMessage> p_failedTcpMsgFlow = new();
  private readonly Subject<byte[]> p_incomingMsgFlow = new();

  public TcpBusClient(IReadOnlyLifetime _lifetime, IScheduler _scheduler, int _port, string _host = "127.0.0.1", string? _password = null)
  {
    var typesCache = new Dictionary<string, Type>();
    foreach (var type in Utilities.GetTypesOf<IBusMsg>())
      typesCache.Add(type.ToString(), type);
    p_typesCache = typesCache;
    p_lifetime = _lifetime;
    p_scheduler = _scheduler;
    p_password = _password != null ? Encoding.UTF8.GetBytes(_password) : null;
    _lifetime.DisposeOnCompleted(p_msgFlow);
    _lifetime.DisposeOnCompleted(p_incomingMsgFlow);

    p_client = _lifetime.DisposeOnCompleted(new WatsonTcpClient(_host, _port))!;
    p_client.Events.MessageReceived += MessageReceivedRaw;
    _lifetime.DoOnCompleted(() => p_client.Events.MessageReceived -= MessageReceivedRaw);

    async Task<bool> sendTcpMsgJob(JobContext<TcpMessage> _ctx)
    {
      try
      {
        if (!p_client.Connected)
          p_client.Connect();

        var data = await _ctx.JobInfo.Job.GetSerializedValue(p_password, _ctx.CancellationToken);
        return await p_client.SendAsync(data, token: _ctx.CancellationToken);
      }
      catch
      {
        return false;
      }
    }
    Task<PenaltyInfo> sendTcpMsgJobPenalty(JobFailContext<TcpMessage> _ctx)
    {
      return Task.FromResult(new PenaltyInfo(_ctx.FailedCounter < 100, TimeSpan.FromMilliseconds(_ctx.FailedCounter * 300))); // 300ms - 30 sec
    }

    WorkerTeam.Run(p_failedTcpMsgFlow, sendTcpMsgJob, sendTcpMsgJobPenalty, _lifetime, 4);

    p_incomingMsgFlow
        .ObserveOn(p_scheduler)
        .SelectAsync(MessageReceived!, p_scheduler)
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

  private async Task MessageReceived(byte[] _msgData, CancellationToken _ct)
  {
    byte[]? bytes;
    if (p_password != null)
      bytes = await Cryptography.DecryptAes(_msgData, p_password, true, _ct);
    else
      bytes = _msgData;

    try
    {
      var json = Encoding.UTF8.GetString(bytes);
      var msg = JsonConvert.DeserializeObject<TcpMessage>(json);

      if (msg == null)
        return;
      if (!p_typesCache.TryGetValue(msg.DataType, out var type))
        return;

      var data = msg.Data.ToObject(type);
      if (data == null)
        return;

      var msgSerial = new BusMsgSerial((IBusMsg)data, msg.Guid);
      p_lastMsg[type] = msgSerial;
      p_msgFlow.OnNext(msgSerial);
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
  public IObservable<T> OfType<T>(bool _includeLastValue = false) where T : IBusMsg
  {
    if (_includeLastValue && p_lastMsg.TryGetValue(typeof(T), out var msg))
      return p_msgFlow
          .Where(x => x.Data.GetType() == typeof(T))
          .Select(x => x.Data)
          .Merge(Observable.Return(msg.Data))
          .Cast<T>()
          .ObserveOn(p_scheduler);
    else
      return p_msgFlow
          .Where(x => x.Data.GetType() == typeof(T))
          .Select(x => x.Data)
          .Cast<T>()
          .ObserveOn(p_scheduler);
  }

  /// <summary>
  /// Get Observable of messages by type
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="includeLastValue"></param>
  /// <returns></returns>
  public IObservable<BusMsgSerial> OfTypeRaw<T>(bool includeLastValue = false) where T : IBusMsg
  {
    if (includeLastValue && p_lastMsg.TryGetValue(typeof(T), out var msg))
      return p_msgFlow
          .Where(x => x.Data.GetType() == typeof(T))
          .Merge(Observable.Return(msg))
          .ObserveOn(p_scheduler);
    else
      return p_msgFlow
          .Where(x => x.Data.GetType() == typeof(T))
          .ObserveOn(p_scheduler);
  }

  /// <summary>
  /// Send message to bus
  /// </summary>
  /// <param name="_data"></param>
  public async Task PostMsg(IBusMsg _data, CancellationToken _ct)
  {
    if (_data == null)
      throw new ArgumentNullException(nameof(_data));

    await PostMsg(new BusMsgSerial(_data, Guid.NewGuid()), _ct);
  }

  public async Task PostMsg(BusMsgSerial _msg, CancellationToken _ct)
  {
    p_lastMsg[_msg.Data.GetType()] = _msg;
    p_msgFlow.OnNext(_msg);

    var tcpMsg = new TcpMessage(
        _msg.Id,
        _msg.Data.GetType().ToString(),
        JToken.FromObject(_msg.Data));

    var json = await tcpMsg.GetSerializedValue(p_password, _ct);

    if (!await p_client.SendAsync(json, token: _ct))
      p_failedTcpMsgFlow.OnNext(tcpMsg);
  }

  public async Task<TRes?> PostReqResOrDefaultAsync<TReq, TRes>(TReq _req, TimeSpan _timeout, CancellationToken _ct)
      where TReq : IBusMsg
      where TRes : IBusMsg
  {
    var guid = Guid.NewGuid();
    var ignoredGuid = Guid.NewGuid();

    try
    {
      var value = await Observable
          .Merge(
              p_msgFlow.ObserveOn(p_scheduler).Where(_x => _x.Id == guid && _x.Data.GetType() == typeof(TRes)),
              Observable.Timer(_timeout, p_scheduler).Select(_ => new BusMsgSerial(new EmptyBusMsg(), Guid.Empty)),
              Observable.Timer(TimeSpan.Zero).SelectAsync(async (_, _ct) =>
              {
                await PostMsg(new BusMsgSerial(_req, guid), _ct);
                return new BusMsgSerial(new EmptyBusMsg(), ignoredGuid);
              }))
          .FirstOrDefaultAsync(_x => _x.Id != ignoredGuid)
          .ToTask(_ct);

      if (value != default && value.Id != Guid.Empty)
        return (TRes)value.Data;

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
      where TReq : IBusMsg
      where TRes : IBusMsg
  {
    return p_msgFlow
        .Where(x => x.Data.GetType() == typeof(TReq))
        .ObserveOn(p_scheduler)
        .SelectAsync(async (_msg, _ct) =>
        {
          var guid = _msg!.Id;
          var result = _func((TReq)_msg.Data);

          await PostMsg(new BusMsgSerial(result, guid), _ct);
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
      where TReq : IBusMsg
      where TRes : IBusMsg
  {
    return p_msgFlow
        .Where(_x => _x.Data.GetType() == typeof(TReq))
        .ObserveOn(p_scheduler)
        .SelectAsync(async (_x, _ct) =>
        {
          var guid = _x!.Id;
          var result = await _func((TReq)_x.Data);

          await PostMsg(new BusMsgSerial(result, guid), _ct);
          return Unit.Default;
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
      where TReq : IBusMsg
      where TRes : IBusMsg
  {
    p_msgFlow
        .Where(x => x.Data.GetType() == typeof(TReq))
        .ObserveOn(p_scheduler)
        .SelectAsync(async (_msg, _ct) =>
        {
          var guid = _msg!.Id;
          var result = _func((TReq)_msg.Data);

          await PostMsg(new BusMsgSerial(result, guid), _ct);
        })
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
      where TReq : IBusMsg
      where TRes : IBusMsg
  {
    p_msgFlow
        .Where(_x => _x.Data.GetType() == typeof(TReq))
        .ObserveOn(p_scheduler)
        .SelectAsync(async (_x, _ct) =>
        {
          var guid = _x!.Id;
          var result = await _func((TReq)_x.Data);

          await PostMsg(new BusMsgSerial(result, guid), _ct);
          return Unit.Default;
        })
        .Subscribe(_lifetime.Token);
  }

}
