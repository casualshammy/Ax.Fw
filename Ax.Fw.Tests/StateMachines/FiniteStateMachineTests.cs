using Ax.Fw.Extensions;
using Ax.Fw.StateMachines;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests.StateMachines;

public class FiniteStateMachineTests
{
  private readonly ITestOutputHelper p_output;

  public FiniteStateMachineTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  private static IStateMachine<int> BuildSimpleMachine(int _initialData = 0)
    => FiniteStateMachineBuilder<int>.Create()
      .WithState("Idle", _default: true)
      .WithState("Running")
      .WithState("Stopped")
      .WithTransition("Idle", "Running")
      .WithTransition("Running", "Stopped")
      .WithTransition("Stopped", "Idle")
      .Build(_initialData);

  // ---- Builder ----

  [Fact]
  public void Builder_WithState_DuplicateName_Throws()
  {
    var builder = FiniteStateMachineBuilder<int>.Create()
      .WithState("A", _default: true);

    Assert.Throws<InvalidOperationException>(() => builder.WithState("A"));
  }

  [Fact]
  public void Builder_WithState_DuplicateDefault_Throws()
  {
    var builder = FiniteStateMachineBuilder<int>.Create()
      .WithState("A", _default: true);

    Assert.Throws<InvalidOperationException>(() => builder.WithState("B", _default: true));
  }

  [Fact]
  public void Builder_WithTransition_SelfTransition_Throws()
  {
    var builder = FiniteStateMachineBuilder<int>.Create()
      .WithState("A", _default: true);

    Assert.Throws<InvalidOperationException>(() => builder.WithTransition("A", "A"));
  }

  [Fact]
  public void Builder_WithTransition_NonExistentFromState_Throws()
  {
    var builder = FiniteStateMachineBuilder<int>.Create()
      .WithState("A", _default: true)
      .WithState("B");

    Assert.Throws<InvalidOperationException>(() => builder.WithTransition("X", "B"));
  }

  [Fact]
  public void Builder_WithTransition_NonExistentToState_Throws()
  {
    var builder = FiniteStateMachineBuilder<int>.Create()
      .WithState("A", _default: true);

    Assert.Throws<InvalidOperationException>(() => builder.WithTransition("A", "X"));
  }

  [Fact]
  public void Builder_Build_NoDefaultState_Throws()
  {
    var builder = FiniteStateMachineBuilder<int>.Create()
      .WithState("A")
      .WithState("B")
      .WithTransition("A", "B");

    Assert.Throws<InvalidOperationException>(() => builder.Build(0));
  }

  [Fact]
  public void Builder_Build_CalledTwice_Throws()
  {
    var builder = FiniteStateMachineBuilder<int>.Create()
      .WithState("A", _default: true);
    builder.Build(0);

    Assert.Throws<InvalidOperationException>(() => builder.Build(0));
  }

  [Fact]
  public void Builder_WithState_AfterBuild_Throws()
  {
    var builder = FiniteStateMachineBuilder<int>.Create()
      .WithState("A", _default: true);
    builder.Build(0);

    Assert.Throws<InvalidOperationException>(() => builder.WithState("B"));
  }

  [Fact]
  public void Builder_WithTransition_AfterBuild_Throws()
  {
    var builder = FiniteStateMachineBuilder<int>.Create()
      .WithState("A", _default: true)
      .WithState("B");
    builder.Build(0);

    Assert.Throws<InvalidOperationException>(() => builder.WithTransition("A", "B"));
  }

  // ---- FSM: initial state ----

  [Fact]
  public void FSM_InitialState_IsDefaultState()
  {
    var fsm = BuildSimpleMachine(42);

    Assert.Equal("Idle", fsm.CurrentState.State);
    Assert.Equal(42, fsm.CurrentState.Data);
  }

  // ---- FSM: transitions ----

  [Fact]
  public void FSM_DoTransition_ChangesCurrentState()
  {
    var fsm = BuildSimpleMachine();

    fsm.DoTransition("Running");

    Assert.Equal("Running", fsm.CurrentState.State);
  }

  [Fact]
  public void FSM_DoTransition_InvalidTransition_Throws()
  {
    var fsm = BuildSimpleMachine();

    // Idle → Stopped is not defined
    Assert.Throws<InvalidOperationException>(() => fsm.DoTransition("Stopped"));
  }

  [Fact]
  public void FSM_DoTransition_InvalidTransition_StateUnchanged()
  {
    var fsm = BuildSimpleMachine(7);

    try { fsm.DoTransition("Stopped"); } catch (InvalidOperationException) { }

    Assert.Equal("Idle", fsm.CurrentState.State);
    Assert.Equal(7, fsm.CurrentState.Data);
  }

  [Fact]
  public void FSM_DoTransition_UnknownState_Throws()
  {
    var fsm = BuildSimpleMachine();

    Assert.Throws<InvalidOperationException>(() => fsm.DoTransition("NonExistent"));
  }

  [Fact]
  public void FSM_DoTransition_NoCallbacks_DataUnchanged()
  {
    var fsm = BuildSimpleMachine(99);

    fsm.DoTransition("Running");

    Assert.Equal(99, fsm.CurrentState.Data);
  }

  [Fact]
  public void FSM_DoTransition_MultipleTransitions_StateAndDataAreCorrect()
  {
    var fsm = BuildSimpleMachine();

    fsm.DoTransition("Running");
    Assert.Equal("Running", fsm.CurrentState.State);

    fsm.DoTransition("Stopped");
    Assert.Equal("Stopped", fsm.CurrentState.State);

    fsm.DoTransition("Idle");
    Assert.Equal("Idle", fsm.CurrentState.State);
  }

  // ---- FSM: callbacks ----

  [Fact]
  public void FSM_DoTransition_OnPreLeave_IsCalled()
  {
    var called = false;
    var fsm = FiniteStateMachineBuilder<int>.Create()
      .WithState("A", _onPreLeave: data => { called = true; return data.Data; }, _default: true)
      .WithState("B")
      .WithTransition("A", "B")
      .Build(0);

    fsm.DoTransition("B");

    Assert.True(called);
  }

  [Fact]
  public void FSM_DoTransition_OnPreEnter_IsCalled()
  {
    var called = false;
    var fsm = FiniteStateMachineBuilder<int>.Create()
      .WithState("A", _default: true)
      .WithState("B", _onPreEnter: data => { called = true; return data.Data; })
      .WithTransition("A", "B")
      .Build(0);

    fsm.DoTransition("B");

    Assert.True(called);
  }

  [Fact]
  public void FSM_DoTransition_OnPreEnter_ReceivesCursorWithNewStateAndPrevious()
  {
    string? seenState = null;
    string? seenPrevious = null;
    var fsm = FiniteStateMachineBuilder<int>.Create()
      .WithState("A", _default: true)
      .WithState("B", _onPreEnter: data =>
      {
        seenState = data.State;
        seenPrevious = data.PreviousState;
        return data.Data;
      })
      .WithTransition("A", "B")
      .Build(0);

    fsm.DoTransition("B");

    Assert.Equal("B", seenState);
    Assert.Equal("A", seenPrevious);
  }

  [Fact]
  public void FSM_DoTransition_OnPreLeave_ModifiesData()
  {
    var fsm = FiniteStateMachineBuilder<int>.Create()
      .WithState("A", _onPreLeave: data => data.Data + 1, _default: true)
      .WithState("B")
      .WithTransition("A", "B")
      .Build(10);

    fsm.DoTransition("B");

    Assert.Equal(11, fsm.CurrentState.Data);
  }

  [Fact]
  public void FSM_DoTransition_OnPreEnter_ModifiesData()
  {
    var fsm = FiniteStateMachineBuilder<int>.Create()
      .WithState("A", _default: true)
      .WithState("B", _onPreEnter: data => data.Data + 1)
      .WithTransition("A", "B")
      .Build(10);

    fsm.DoTransition("B");

    Assert.Equal(11, fsm.CurrentState.Data);
  }

  [Fact]
  public void FSM_DoTransition_BothCallbacks_DataFlowIsOnPreLeaveFirst()
  {
    // Data must flow: initial → OnPreLeave(A) → OnPreEnter(B) → CurrentState.Data
    var fsm = FiniteStateMachineBuilder<int>.Create()
      .WithState("A", _onPreLeave: data => data.Data + 10, _default: true)
      .WithState("B", _onPreEnter: data => data.Data * 2)
      .WithTransition("A", "B")
      .Build(5);

    fsm.DoTransition("B");

    Assert.Equal((5 + 10) * 2, fsm.CurrentState.Data);
  }

  [Fact]
  public void FSM_DoTransition_CallbacksNotCalledOnFailedTransition()
  {
    var leaveCalled = false;
    var fsm = FiniteStateMachineBuilder<int>.Create()
      .WithState("A", _onPreLeave: data => { leaveCalled = true; return data.Data; }, _default: true)
      .WithState("B")
      .WithTransition("A", "B")
      .Build(0);

    // Idle → Stopped is invalid, callbacks must not fire
    try { fsm.DoTransition("Stopped"); } catch (InvalidOperationException) { }

    Assert.False(leaveCalled);
  }

  // ---- FSM: observers ----

  [Fact]
  public async Task FSM_Subscribe_ImmediatelyReceivesCurrentCursorAsync()
  {
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    var fsm = BuildSimpleMachine(123);
    var received = await fsm.StateTransition.FirstOrDefaultAsync(cts.Token);

    Assert.NotNull(received);
    Assert.Equal("Idle", received.State);
    Assert.Equal(123, received.Data);
    Assert.Null(received.PreviousState);
  }

  [Fact]
  public void FSM_DoTransition_NotifiesObserversWithNewCursor()
  {
    var fsm = BuildSimpleMachine();
    var cursors = new List<StateMachineCursor<int>>();

    fsm.StateTransition.Subscribe(cursors.Add);
    fsm.DoTransition("Running");

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    while (!cts.IsCancellationRequested)
      if (cursors.Count == 2)
        break;

    // First entry is the initial snapshot, second is the transition
    Assert.Equal(2, cursors.Count);
    Assert.Equal("Idle", cursors[0].State);
    Assert.Equal("Running", cursors[1].State);
    Assert.Equal("Idle", cursors[1].PreviousState);
  }

  [Fact]
  public void FSM_Subscribe_AfterTransition_DoesNotReceivePastTransitions()
  {
    var fsm = BuildSimpleMachine();
    fsm.DoTransition("Running");

    var cursors = new List<StateMachineCursor<int>>();
    using (fsm.StateTransition.Subscribe(cursors.Add))
    {
    }

    Assert.Single(cursors);
    Assert.Equal("Running", cursors[0].State);
  }

  [Fact]
  public void FSM_Subscribe_Dispose_StopsReceivingNotifications()
  {
    var fsm = BuildSimpleMachine();
    var cursors = new List<StateMachineCursor<int>>();

    var sub = fsm.StateTransition.Subscribe(cursors.Add);
    fsm.DoTransition("Running");

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    while (!cts.IsCancellationRequested)
      if (cursors.Count == 2)
        break;

    sub.Dispose();
    fsm.DoTransition("Stopped");
    fsm.DoTransition("Idle");

    // initial + one transition
    Assert.Equal(2, cursors.Count);
    Assert.Equal("Running", cursors[^1].State);
  }

  [Fact]
  public void FSM_Subscribe_MultipleObservers_AllNotified()
  {
    var fsm = BuildSimpleMachine();
    var a = new List<StateMachineCursor<int>>();
    var b = new List<StateMachineCursor<int>>();

    using (fsm.StateTransition.Subscribe(a.Add))
    using (fsm.StateTransition.Subscribe(b.Add))
    {
      fsm.DoTransition("Running");

      using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
      while (!cts.IsCancellationRequested)
        if (a.Count == 2 && b.Count == 2)
          break;
    }

    Assert.Equal(2, a.Count);
    Assert.Equal(2, b.Count);
    Assert.Equal(a, b);
  }

  [Fact]
  public void FSM_DoTransition_ObserverThrows_DoesNotBreakTransition()
  {
    var fsm = BuildSimpleMachine();
    var otherReceived = 0;
    var bad = new BadObserver();

    fsm.StateTransition.Subscribe(bad);
    fsm.StateTransition.Subscribe(_ => otherReceived++);
    fsm.DoTransition("Running");

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    while (!cts.IsCancellationRequested)
      if (otherReceived == 2)
        break;

    Assert.Equal("Running", fsm.CurrentState.State);
    Assert.Equal(2, otherReceived);
  }

  private sealed class BadObserver : IObserver<StateMachineCursor<int>>
  {
    public void OnNext(StateMachineCursor<int> _value) => throw new InvalidOperationException("boom");
    public void OnError(Exception _error) { }
    public void OnCompleted() { }
  }

  // ---- FSM: thread safety ----

  [Fact(Timeout = 10000)]
  public async Task FSM_DoTransition_ConcurrentCalls_NoCorruption()
  {
    var fsm = FiniteStateMachineBuilder<int>.Create()
      .WithState("A", _onPreLeave: d => d.Data + 1, _default: true)
      .WithState("B", _onPreLeave: d => d.Data + 1)
      .WithTransition("A", "B")
      .WithTransition("B", "A")
      .Build(0);

    const int ITERATIONS = 500;
    var unexpectedExceptions = new List<Exception>();
    var barrier = new Barrier(2);

    void Run()
    {
      barrier.SignalAndWait();
      for (var i = 0; i < ITERATIONS; i++)
      {
        try
        {
          var target = fsm.CurrentState.State == "A" ? "B" : "A";
          fsm.DoTransition(target);
        }
        catch (InvalidOperationException)
        {
          // Expected: another thread may have already transitioned
        }
        catch (Exception ex)
        {
          lock (unexpectedExceptions)
            unexpectedExceptions.Add(ex);
        }
      }
    }

    var t1 = Task.Run(Run);
    var t2 = Task.Run(Run);
    await Task.WhenAll(t1, t2);

    Assert.Empty(unexpectedExceptions);
    Assert.Contains(fsm.CurrentState.State, (IEnumerable<string>)["A", "B"]);
    Assert.True(fsm.CurrentState.Data >= 0);
  }

}
