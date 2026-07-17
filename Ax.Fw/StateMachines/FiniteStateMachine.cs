using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Channels;
using Ax.Fw.Extensions;

namespace Ax.Fw.StateMachines;

/// <summary>
/// Represents a finite state machine that holds associated data of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of data associated with the state machine. Must be non-null.</typeparam>
public interface IStateMachine<T>
  where T : notnull
{
  /// <summary>
  /// Gets the current state and associated data of the state machine.
  /// </summary>
  public StateMachineCursor<T> CurrentState { get; }

  /// <summary>
  /// An observable that emits a <see cref="StateMachineCursor{T}"/> upon subscription and after each subsequent transition.
  /// </summary>
  public IObservable<StateMachineCursor<T>> StateTransition { get; }

  /// <summary>
  /// Performs a transition to the specified state.
  /// </summary>
  /// <param name="_toState">The name of the target state.</param>
  /// <exception cref="InvalidOperationException">
  /// Thrown when the transition from the current state to <paramref name="_toState"/> is not allowed.
  /// </exception>
  public void DoTransition(string _toState);
}

/// <summary>
/// Represents a snapshot of the state machine at a point in time,
/// containing the current state name and associated data.
/// </summary>
/// <typeparam name="T">The type of data associated with the state machine. Must be non-null.</typeparam>
/// <param name="Data">The data associated with the current state.</param>
/// <param name="State">The name of the current state.</param>
/// <param name="PreviousState">The name of the previous state, or <see langword="null"/> if there is no previous state.</param>
public sealed record StateMachineCursor<T>(T Data, string State, string? PreviousState)
  where T : notnull;

internal record FiniteStateMachineState<T>(
  string Name,
  Func<StateMachineCursor<T>, T>? OnPreEnter,
  Func<StateMachineCursor<T>, T>? OnPreLeave,
  FrozenSet<string> Transitions,
  bool Default)
  where T : notnull;

/// <summary>
/// A builder for constructing a <see cref="FiniteStateMachine{T}"/>.
/// Each builder instance can only be used to build a single state machine.
/// <br/>
/// This class is not thread-safe; all configuration must be done from a single thread.
/// </summary>
/// <typeparam name="T">The type of data associated with the state machine. Must be non-null.</typeparam>
public sealed class FiniteStateMachineBuilder<T>
  where T : notnull
{
  private readonly Dictionary<string, FiniteStateMachineState<T>> p_states = new();
  private long p_built = 0;

  /// <summary>
  /// Creates a new <see cref="FiniteStateMachineBuilder{T}"/> instance.
  /// </summary>
  public static FiniteStateMachineBuilder<T> Create()
    => new();

  /// <summary>
  /// Registers a state with the given name.
  /// </summary>
  /// <param name="_stateName">The unique name of the state.</param>
  /// <param name="_onPreEnter">
  /// An optional callback invoked before entering this state.
  /// Receives the current data and returns the updated data.
  /// </param>
  /// <param name="_onPreLeave">
  /// An optional callback invoked before leaving this state.
  /// Receives the current data and returns the updated data.
  /// </param>
  /// <param name="_default">If <see langword="true"/>, this state is used as the initial state. Only one default state is allowed.</param>
  /// <returns>The current builder instance for chaining.</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown when a state with the same name already exists, a default state is already defined, or the builder has already been used.
  /// </exception>
  public FiniteStateMachineBuilder<T> WithState(
    string _stateName,
    Func<StateMachineCursor<T>, T>? _onPreEnter = null,
    Func<StateMachineCursor<T>, T>? _onPreLeave = null,
    bool _default = false)
  {
    if (Interlocked.Read(ref p_built) == 1)
      throw new InvalidOperationException($"This builder has already been used to build a state machine.");

    if (p_states.TryGetValue(_stateName, out _))
      throw new InvalidOperationException($"State with name '{_stateName}' already exists.");

    if (_default && p_states.Values.Any(_ => _.Default))
      throw new InvalidOperationException($"Default state is already defined.");

    var state = new FiniteStateMachineState<T>(_stateName, _onPreEnter, _onPreLeave, [], _default);
    p_states[_stateName] = state;
    return this;
  }

  /// <summary>
  /// Registers an allowed transition between two states.
  /// </summary>
  /// <param name="_fromState">The name of the source state.</param>
  /// <param name="_toState">The name of the target state.</param>
  /// <returns>The current builder instance for chaining.</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown when either state does not exist, the source and target states are the same, or the builder has already been used.
  /// </exception>
  public FiniteStateMachineBuilder<T> WithTransition(
    string _fromState,
    string _toState)
  {
    if (Interlocked.Read(ref p_built) == 1)
      throw new InvalidOperationException($"This builder has already been used to build a state machine.");

    if (_fromState == _toState)
      throw new InvalidOperationException($"Transition from a state to itself is not allowed.");

    var fromState = p_states.GetValueOrDefault(_fromState)
      ?? throw new InvalidOperationException($"State with name '{_fromState}' does not exist.");
    var toState = p_states.GetValueOrDefault(_toState)
      ?? throw new InvalidOperationException($"State with name '{_toState}' does not exist.");

    var newTransitions = new HashSet<string>(fromState.Transitions)
    {
      toState.Name
    };
    p_states[_fromState] = fromState with
    {
      Transitions = newTransitions.ToFrozenSet()
    };

    return this;
  }

  /// <summary>
  /// Builds and returns the state machine with the specified initial data.
  /// This method can only be called once per builder instance.
  /// The <c>OnPreEnter</c> callback of the default state is not invoked — the initial
  /// <see cref="StateMachineCursor{T}"/> is created directly from <paramref name="_data"/>.
  /// </summary>
  /// <param name="_data">The initial data associated with the default state.</param>
  /// <returns>A new <see cref="IStateMachine{T}"/> instance.</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown when no default state has been defined or the builder has already been used.
  /// </exception>
  public IStateMachine<T> Build(T _data)
  {
    var alreadyBuilt = Interlocked.Exchange(ref p_built, 1);

    if (alreadyBuilt == 1)
      throw new InvalidOperationException($"This builder has already been used to build a state machine.");

    var defaultState = p_states.Values.FirstOrDefault(_ => _.Default)
      ?? throw new InvalidOperationException($"At least one state must be marked as default.");

    var currentState = new StateMachineCursor<T>(_data, defaultState.Name, null);
    return new FiniteStateMachine<T>(p_states, currentState);
  }

}

/// <summary>
/// A thread-safe finite state machine that holds associated data of type <typeparamref name="T"/>.
/// Use <see cref="FiniteStateMachineBuilder{T}"/> to create an instance.
/// </summary>
/// <typeparam name="T">The type of data associated with the state machine. Must be non-null.</typeparam>
public sealed class FiniteStateMachine<T> : IStateMachine<T>
  where T : notnull
{
  private readonly FrozenDictionary<string, FiniteStateMachineState<T>> p_states;
  private readonly ReplaySubject<StateMachineCursor<T>> p_stateSubj = new(1);
  private readonly object p_lock = new();

  internal FiniteStateMachine(
    IReadOnlyDictionary<string, FiniteStateMachineState<T>> _states,
    StateMachineCursor<T> _currentState)
  {
    p_states = _states.ToFrozenDictionary();
    p_stateSubj.OnNext(_currentState);
    CurrentState = _currentState;
    StateTransition = p_stateSubj.ObserveOnThreadPool();
  }

  /// <inheritdoc/>
  public StateMachineCursor<T> CurrentState { get; private set; }

  /// <inheritdoc/>
  public IObservable<StateMachineCursor<T>> StateTransition { get; }

  /// <inheritdoc/>
  /// <remarks>
  /// The <c>OnPreLeave</c> callback of the current state is invoked first,
  /// followed by the <c>OnPreEnter</c> callback of the target state.
  /// <c>OnPreEnter</c> receives a cursor whose <c>State</c> is the new state
  /// and whose <c>PreviousState</c> is the state being left.
  /// Both callbacks run inside the transition lock — do not call <see cref="DoTransition"/>
  /// on the same instance from within a callback, as this will cause a deadlock.
  /// </remarks>
  public void DoTransition(string _toState)
  {
    lock (p_lock)
    {
      var currentState = p_states[CurrentState.State];
      if (!currentState.Transitions.Contains(_toState))
        throw new InvalidOperationException($"Transition from '{currentState.Name}' to '{_toState}' is not allowed.");

      var nextState = p_states[_toState];

      var newData = currentState.OnPreLeave != null
        ? currentState.OnPreLeave(CurrentState)
        : CurrentState.Data;

      newData = nextState.OnPreEnter != null
        ? nextState.OnPreEnter(new StateMachineCursor<T>(newData, nextState.Name, currentState.Name))
        : newData;

      var newCursor = new StateMachineCursor<T>(newData, nextState.Name, currentState.Name);
      CurrentState = newCursor;
      p_stateSubj.OnNext(newCursor);
    }
  }

}
