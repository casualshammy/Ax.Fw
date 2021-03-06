#nullable enable
using Ax.Fw.Bus;
using Ax.Fw.Extensions;
using Ax.Fw.PipeBus.Data;
using Ax.Fw.SharedTypes.Data.Bus;
using Ax.Fw.SharedTypes.Data.Workers;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Workers;
using H.Pipes;
using H.Pipes.Args;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;

namespace Ax.Fw.PipeBus;

public class PipeBusServer : IPipeBus
{
    private readonly PipeServer<PipeMsg> p_server;
    private ImmutableHashSet<PipeConnection<PipeMsg>> p_clients = ImmutableHashSet<PipeConnection<PipeMsg>>.Empty;
    private readonly Subject<BusMsgSerial> p_msgFlow = new();
    private readonly ConcurrentDictionary<Type, BusMsgSerial> p_lastMsg = new();
    private readonly IScheduler p_scheduler;
    private readonly IReadOnlyDictionary<string, Type> p_typesCache;
    private readonly bool p_includeClient;
    private readonly Subject<(PipeConnection<PipeMsg>, PipeMsg Msg)> p_failedTcpMsgFlow = new();

    internal PipeBusServer(PipeServer<PipeMsg> _server, ILifetime _lifetime, IScheduler _scheduler, bool _includeClient)
    {
        PipeName = _server.PipeName;
        var typesCache = new Dictionary<string, Type>();
        foreach (var type in Utilities.GetTypesOf<IBusMsg>())
            typesCache.Add(type.ToString(), type);
        p_typesCache = typesCache;

        p_scheduler = _scheduler;
        _lifetime.DisposeOnCompleted(p_msgFlow);
        p_includeClient = _includeClient;

        p_server = _server;
        p_server.ClientConnected += ClientConnected;
        p_server.ClientDisconnected += ClientDisconnected;
        p_server.MessageReceived += MessageReceived;

        async Task<bool> sendTcpMsgJob(JobContext<(PipeConnection<PipeMsg> Connection, PipeMsg Msg)> _ctx)
        {
            try
            {
                await _ctx.JobInfo.Job.Connection.WriteAsync(_ctx.JobInfo.Job.Msg, _ctx.CancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }
        Task<PenaltyInfo> sendTcpMsgJobPenalty(JobFailContext<(PipeConnection<PipeMsg> Connection, PipeMsg Msg)> _ctx)
        {
            return Task.FromResult(new PenaltyInfo(_ctx.FailedCounter < 100, TimeSpan.FromMilliseconds(_ctx.FailedCounter * 300))); // 300ms - 30 sec
        }

        var lifetime = new Lifetime();
        _lifetime.DoOnCompleted(lifetime.Complete);
        WorkerTeam.Run(p_failedTcpMsgFlow, sendTcpMsgJob, sendTcpMsgJobPenalty, lifetime, 4);
    }

    public static async Task<PipeBusServer> RunAsync(ILifetime _lifetime, IScheduler _scheduler, string _pipeName, bool _includeClient)
    {
        var server = _lifetime.DisposeAsyncOnCompleted(new PipeServer<PipeMsg>(_pipeName));
        var pipeBusServer = new PipeBusServer(server, _lifetime, _scheduler, _includeClient);
        await server.StartAsync(_lifetime.Token);
        return pipeBusServer;
    }

    public static PipeBusServer Run(ILifetime _lifetime, IScheduler _scheduler, string _pipeName, bool _includeClient)
    {
        var server = _lifetime.DisposeAsyncOnCompleted(new PipeServer<PipeMsg>(_pipeName));
        var pipeBusServer = new PipeBusServer(server, _lifetime, _scheduler, _includeClient);

        var mre = new ManualResetEvent(false);
        Observable
            .Return(Unit.Default)
            .ObserveOn(ThreadPoolScheduler.Instance)
            .SelectAsync(async (_, _ct) =>
            {
                await server.StartAsync(_ct);
                mre.Set();
            })
            .Subscribe(_lifetime.Token);

        if (!mre.WaitOne(TimeSpan.FromSeconds(5)))
        {
            server.DisposeAsync();
            throw new InvalidOperationException($"Can't start server!");
        }
        return pipeBusServer;
    }

    public bool Connected => p_server.IsStarted;
    public string PipeName { get; }

    private void ClientConnected(object? sender, ConnectionEventArgs<PipeMsg> args)
    {
        p_clients = p_clients.Add(args.Connection);
    }

    private void ClientDisconnected(object? sender, ConnectionEventArgs<PipeMsg> args)
    {
        p_clients = p_clients.Remove(args.Connection);
    }

    private async void MessageReceived(object? sender, ConnectionMessageEventArgs<PipeMsg?> args)
    {
        var pipeMsg = args?.Message;
        if (pipeMsg == null)
            return;

        if (p_includeClient)
        {
            if (pipeMsg?.Type == null || !p_typesCache.TryGetValue(pipeMsg.Type, out var type))
                return;

            var json = pipeMsg.JsonData;
            if (json == null)
                return;

            if (JsonConvert.DeserializeObject(json, type) is not IBusMsg userData)
                return;

            var msgSerial = new BusMsgSerial(userData, pipeMsg.Guid);
            p_lastMsg[type] = msgSerial;
            p_msgFlow.OnNext(msgSerial);
        }

        foreach (var connection in p_clients)
            try
            {
                await p_server.WriteAsync(pipeMsg);
            }
            catch
            {
                p_failedTcpMsgFlow.OnNext((connection, pipeMsg));
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
        if (!p_includeClient)
            throw new InvalidOperationException($"Client features is not enabled! See constructor parameters.");

        if (includeLastValue && p_lastMsg.TryGetValue(typeof(T), out var msg))
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
    public async Task PostMsg(IBusMsg _data)
    {
        if (!p_includeClient)
            throw new InvalidOperationException($"Client features is not enabled! See constructor parameters.");

        if (_data == null)
            throw new ArgumentNullException(nameof(_data));

        await PostMsg(new BusMsgSerial(_data, Guid.NewGuid()));
    }

    public async Task PostMsg(BusMsgSerial _msg)
    {
        p_lastMsg[_msg.Data.GetType()] = _msg;
        p_msgFlow.OnNext(_msg);
        var data = JsonConvert.SerializeObject(_msg.Data);
        var pipeMsg = new PipeMsg(_msg.Id, _msg.Data.GetType().ToString(), data);
        foreach (var connection in p_clients)
        {
            try
            {
                await p_server.WriteAsync(pipeMsg);
            }
            catch
            {
                p_failedTcpMsgFlow.OnNext((connection, pipeMsg));
            }
        }
    }

    public async Task<TRes?> PostReqResOrDefaultAsync<TReq, TRes>(TReq _req, TimeSpan _timeout, CancellationToken _ct)
        where TReq : IBusMsg
        where TRes : IBusMsg
    {
        if (!p_includeClient)
            throw new InvalidOperationException($"Client features is not enabled! See constructor parameters.");

        var guid = Guid.NewGuid();
        var ignoredGuid = Guid.NewGuid();

        try
        {
            var value = await Observable
                .Merge(
                    p_msgFlow.Where(_x => _x.Id == guid && _x.Data.GetType() == typeof(TRes)),
                    Observable.Timer(_timeout).Select(_ => new BusMsgSerial(new EmptyBusMsg(), Guid.Empty)),
                    Observable.Timer(TimeSpan.Zero).SelectAsync(async _ =>
                    {
                        await PostMsg(new BusMsgSerial(_req, guid));
                        return new BusMsgSerial(new EmptyBusMsg(), ignoredGuid);
                    }).Select(_x => _x!))
                .ObserveOn(p_scheduler)
                .FirstOrDefaultAsync(_x => _x.Id != ignoredGuid).ToTask(_ct);

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
        if (!p_includeClient)
            throw new InvalidOperationException($"Client features is not enabled! See constructor parameters.");

        return p_msgFlow
            .Where(x => x.Data.GetType() == typeof(TReq))
            .SelectAsync(async _x =>
            {
                var guid = _x!.Id;
                var result = _func((TReq)_x.Data);

                await PostMsg(new BusMsgSerial(result, guid));
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
        if (!p_includeClient)
            throw new InvalidOperationException($"Client features is not enabled! See constructor parameters.");

        return p_msgFlow
            .Where(_x => _x.Data.GetType() == typeof(TReq))
            .ObserveOn(p_scheduler)
            .SelectAsync(async _x =>
            {
                var guid = _x!.Id;
                var result = await _func((TReq)_x.Data);

                await PostMsg(new BusMsgSerial(result, guid));
                return Unit.Default;
            })
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
        if (!p_includeClient)
            throw new InvalidOperationException($"Client features is not enabled! See constructor parameters.");

        p_msgFlow
            .Where(x => x.Data.GetType() == typeof(TReq))
            .SelectAsync(async _x =>
            {
                var guid = _x!.Id;
                var result = _func((TReq)_x.Data);

                await PostMsg(new BusMsgSerial(result, guid));
            }, p_scheduler)
            .Subscribe(_lifetime.Token);
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
        if (!p_includeClient)
            throw new InvalidOperationException($"Client features is not enabled! See constructor parameters.");

        p_msgFlow
            .Where(_x => _x.Data.GetType() == typeof(TReq))
            .ObserveOn(p_scheduler)
            .SelectAsync(async _x =>
            {
                var guid = _x!.Id;
                var result = await _func((TReq)_x.Data);

                await PostMsg(new BusMsgSerial(result, guid));
                return Unit.Default;
            })
            .Subscribe(_lifetime.Token);
    }

}
