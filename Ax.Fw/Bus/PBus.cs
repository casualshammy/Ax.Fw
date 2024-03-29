﻿using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Data.Bus;
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Bus;

/// <summary>
/// Class what provides message-way of communication between classes
/// </summary>
public class PBus : IBus
{
  private readonly Subject<BusMsgSerial> p_msgFlow = new();
  private readonly ILifetime p_lifetime;
  private readonly IScheduler p_scheduler;
  private readonly ConcurrentDictionary<Type, IBusMsg> p_lastMsg = new();

  public PBus(ILifetime _lifetime) : this(_lifetime, ThreadPoolScheduler.Instance)
  {
  }

  public PBus(ILifetime _lifetime, IScheduler _scheduler)
  {
    p_lifetime = _lifetime;
    p_scheduler = _scheduler;
    _lifetime.ToDisposeOnEnding(p_msgFlow);
  }

  /// <summary>
  /// Send message to bus
  /// </summary>
  /// <param name="_data"></param>
  public void PostMsg(IBusMsg _data)
  {
    if (_data == null)
      throw new ArgumentNullException(nameof(_data));

    PostMsg(new BusMsgSerial(_data, Guid.NewGuid()));
  }

  private void PostMsg(BusMsgSerial _msg)
  {
    if (!p_lifetime.IsCancellationRequested)
    {
      p_lastMsg.AddOrUpdate(_msg.Data.GetType(), _msg.Data, (_, _) => _msg.Data);
      p_msgFlow.OnNext(_msg);
    }
  }

  /// <summary>
  /// Get Observable of messages by type
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="includeLastValue"></param>
  /// <returns></returns>
  public IObservable<T> OfType<T>(bool includeLastValue = false) where T : IBusMsg
  {
    if (includeLastValue && p_lastMsg.TryGetValue(typeof(T), out var msg))
      return p_msgFlow
          .ObserveOn(p_scheduler)
          .Where(x => x.Data.GetType() == typeof(T))
          .Select(x => x.Data)
          .Merge(Observable.Return(msg))
          .Cast<T>();
    else
      return p_msgFlow
          .ObserveOn(p_scheduler)
          .Where(x => x.Data.GetType() == typeof(T))
          .Select(x => x.Data)
          .Cast<T>();
  }

  /// <summary>
  /// Send message and wait for answer
  /// </summary>
  /// <typeparam name="TReq"></typeparam>
  /// <typeparam name="TRes"></typeparam>
  /// <param name="_req"></param>
  /// <param name="_timeout"></param>
  /// <returns></returns>
  public TRes PostReqRes<TReq, TRes>(TReq _req, TimeSpan _timeout)
      where TReq : IBusMsg
      where TRes : IBusMsg
  {
    var result = PostReqResOrDefault<TReq, TRes>(_req, _timeout);
    return result ?? throw new TimeoutException();
  }

  /// <summary>
  /// Send message and wait for answer
  /// </summary>
  /// <typeparam name="TReq"></typeparam>
  /// <typeparam name="TRes"></typeparam>
  /// <param name="_req"></param>
  /// <param name="_timeout"></param>
  /// <returns></returns>
  public TRes? PostReqResOrDefault<TReq, TRes>(TReq _req, TimeSpan _timeout)
      where TReq : IBusMsg
      where TRes : IBusMsg
  {
    var mre = new ManualResetEvent(false);
    var guid = Guid.NewGuid();
    TRes? result = default;
    using var subscription = p_msgFlow
        .ObserveOn(p_scheduler)
        .Where(_x => _x.Id == guid && _x.Data.GetType() == typeof(TRes))
        .Subscribe(_x =>
        {
          result = (TRes)_x.Data;
          mre.Set();
        });
    PostMsg(new BusMsgSerial(_req, guid));
    if (mre.WaitOne(_timeout))
      return result;

    return default;
  }

  public async Task<TRes?> PostReqResOrDefaultAsync<TReq, TRes>(TReq _req, TimeSpan _timeout, CancellationToken _ct)
      where TReq : IBusMsg
      where TRes : IBusMsg
  {
    var guid = Guid.NewGuid();
    var ignoredGuid = Guid.NewGuid();

    //var value = await Observable
    //    .Merge(
    //        p_msgFlow.Where(_x => _x.Id == guid && _x.Data.GetType() == typeof(TRes)),
    //        Observable.Timer(_timeout).Select(_ => new BusMsgSerial(new EmptyBusMsg(), Guid.Empty)),
    //        Observable.Timer(TimeSpan.Zero).Select(_ =>
    //        {
    //            PostMsg(new BusMsgSerial(_req, guid));
    //            return new BusMsgSerial(new EmptyBusMsg(), ignoredGuid);
    //        }))
    //    .ObserveOn(p_scheduler)
    //    .FirstOrDefaultAsync(_x => _x.Id != ignoredGuid);

    try
    {
      var value = await TaskObservableExtensions.ToTask(Observable
          .Merge(
              p_msgFlow.ObserveOn(p_scheduler).Where(_x => _x.Id == guid && _x.Data.GetType() == typeof(TRes)),
              Observable.Timer(_timeout).Select(_ => new BusMsgSerial(new EmptyBusMsg(), Guid.Empty)),
              Observable.Timer(TimeSpan.Zero).Select(_ =>
              {
                PostMsg(new BusMsgSerial(_req, guid));
                return new BusMsgSerial(new EmptyBusMsg(), ignoredGuid);
              }))
          .FirstOrDefaultAsync(_x => _x.Id != ignoredGuid), _ct);

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
  public IDisposable OfReqRes<TReq, TRes>(Func<TReq, Task<TRes>> _func)
      where TReq : IBusMsg
      where TRes : IBusMsg
  {
    return p_msgFlow
        .ObserveOn(p_scheduler)
        .Where(_x => _x.Data.GetType() == typeof(TReq))
        .SelectAsync(async _x =>
        {
          var guid = _x!.Id;
          var result = await _func((TReq)_x.Data);

          PostMsg(new BusMsgSerial(result, guid));
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
  public IDisposable OfReqRes<TReq, TRes>(Func<TReq, TRes> _func)
      where TReq : IBusMsg
      where TRes : IBusMsg
  {
    return p_msgFlow
        .ObserveOn(p_scheduler)
        .Where(x => x.Data.GetType() == typeof(TReq))
        .Subscribe(x =>
        {
          var guid = x.Id;
          var result = _func((TReq)x.Data);

          PostMsg(new BusMsgSerial(result, guid));
        });
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
        .ObserveOn(p_scheduler)
        .Where(_x => _x.Data.GetType() == typeof(TReq))
        .SelectAsync(async _x =>
        {
          var guid = _x!.Id;
          var result = await _func((TReq)_x.Data);

          PostMsg(new BusMsgSerial(result, guid));
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
  public void OfReqRes<TReq, TRes>(Func<TReq, TRes> _func, ILifetime _lifetime)
      where TReq : IBusMsg
      where TRes : IBusMsg
  {
    p_msgFlow
        .ObserveOn(p_scheduler)
        .Where(x => x.Data.GetType() == typeof(TReq))
        .Subscribe(x =>
        {
          var guid = x.Id;
          var result = _func((TReq)x.Data);

          PostMsg(new BusMsgSerial(result, guid));
        }, _lifetime.Token);
  }

}

public class EmptyBusMsg : IBusMsg { }
