// ===================================================================================
// Signals - A typesafe, lightweight messaging lib for Unity.
// ===================================================================================
// 2017, Yanko Oliveira  / http://yankooliveira.com / http://twitter.com/yankooliveira
// Special thanks to Max Knoblich for code review and Aswhin Sudhir for the anonymous
// function asserts suggestion.
// ===================================================================================
// Inspired by StrangeIOC, minus the clutter.
// Based on http://wiki.unity3d.com/index.php/CSharpMessenger_Extended
// Converted to use strongly typed parameters and prevent use of strings as ids.
//
// Supports up to 3 parameters. More than that, and you should probably use a VO.
// ===================================================================================
// Modified by Turner Monroe 2025
// Added ISignalListener interface to attempt to enforce cleanup of subscriptions
// when the listener is destroyed.
//
// Added Editor Features/Flags:
// - Auto-pruning of dead UnityEngine.Object listeners (those whose targets have been destroyed)
// - Detailed dispatch logging
// - Strict mode that throws on dead listeners instead of just logging them
// ===================================================================================

using System;
using System.Collections.Generic;

/// <summary>
/// Base interface for Signals
/// </summary>
public interface ISignal
{
    string Hash { get; }
}

/// <summary>
/// Interface for anything that owns signal subscriptions and must clean them up.
/// Implementors MUST call UnsubscribeAllSignals() at the end of their lifetime
/// (OnDestroy / Dispose / etc.).
/// </summary>
public interface ISignalListener
{
    void UnsubscribeAllSignals();
}

/// <summary>
/// Extension methods for ISignalListener
/// convinece method to clear all signal subscriptions
/// from this listener
///
/// Call like
/// UnsubscribeAllSignals() {
///    this.ClearSignalSubscriptions();
/// }
/// </summary>
public static class SignalListenerExtensions
{
    public static void ClearSignalSubscriptions(this ISignalListener owner)
    {
        SignalLifetimeRegistry.UnsubscribeAll(owner);
    }
}

/// Editor-only interface to expose debug info
#if UNITY_EDITOR
public interface ISignalDebugInfo
{
    int DebugListenerCount { get; }
}
#endif

/// <summary>
/// Central registry that tracks cleanup actions for each ISignalListener.
/// Signals register "how to unsubscribe" here when AddListener is called.
/// </summary>
internal static class SignalLifetimeRegistry
{
    private static readonly Dictionary<ISignalListener, List<Action>> _cleanups =
        new Dictionary<ISignalListener, List<Action>>();

    public static void Register(ISignalListener owner, Action cleanup)
    {
        if (owner == null || cleanup == null)
        {
            return;
        }

        if (!_cleanups.TryGetValue(owner, out var list))
        {
            list = new List<Action>();
            _cleanups[owner] = list;
        }

        list.Add(cleanup);
    }

    public static void UnsubscribeAll(ISignalListener owner)
    {
        if (owner == null)
        {
            return;
        }

        if (!_cleanups.TryGetValue(owner, out var list))
        {
            return;
        }

        // Execute all stored cleanup actions (which call RemoveListener internally)
        for (int i = list.Count - 1; i >= 0; i--)
        {
            list[i]?.Invoke();
        }

        list.Clear();
        _cleanups.Remove(owner);
    }
}

/// <summary>
/// Signals main facade class
/// </summary>
public class Signals
{
    private static Dictionary<Type, ISignal> signals = new Dictionary<Type, ISignal>();

#if UNITY_EDITOR
    /// <summary>
    /// When true, Signals will attempt to auto-prune obviously dead Unity listeners
    /// (destroyed UnityEngine.Object targets) and log warnings when dispatching.
    /// </summary>
    public static bool EnableDebugPruning = true;

    /// <summary>
    /// When true, Signals will log detailed dispatch information in the editor:
    /// which signal, in what order, and which method/target was invoked.
    /// </summary>
    public static bool EnableDebugDispatchLogging = false;

    /// <summary>
    /// When true, encountering a dead Unity listener will throw instead of just logging
    /// and pruning it. Editor-only strict mode for catching lifetime bugs.
    /// </summary>
    public static bool ThrowOnDeadListener = false;
#endif

    /// <summary>
    /// Getter for a signal of a given type
    /// </summary>
    /// <typeparam name="SType">Type of signal</typeparam>
    /// <returns>The proper signal binding</returns>
    public static SType Get<SType>()
        where SType : ISignal, new()
    {
        Type signalType = typeof(SType);
        ISignal signal;

        if (signals.TryGetValue(signalType, out signal))
        {
            return (SType)signal;
        }

        return (SType)Bind(signalType);
    }

    /// <summary>
    /// Manually provide a SignalHash and bind it to a given listener
    /// (you most likely want to use an AddListener, unless you know exactly
    /// what you are doing)
    /// </summary>
    /// <param name="signalHash">Unique hash for signal</param>
    /// <param name="owner">Owner responsible for cleanup</param>
    /// <param name="handler">Callback for signal listener</param>
    public static void AddListenerToHash(string signalHash, ISignalListener owner, Action handler)
    {
        ISignal signal = GetSignalByHash(signalHash);
        if (signal != null && signal is ASignal)
        {
            (signal as ASignal).AddListener(owner, handler);
        }
    }

    /// <summary>
    /// Manually provide a SignalHash and unbind it from a given listener
    /// (you most likely want to use a RemoveListener, unless you know exactly
    /// what you are doing)
    /// </summary>
    /// <param name="signalHash">Unique hash for signal</param>
    /// <param name="handler">Callback for signal listener</param>
    public static void RemoveListenerFromHash(string signalHash, Action handler)
    {
        ISignal signal = GetSignalByHash(signalHash);
        if (signal != null && signal is ASignal)
        {
            (signal as ASignal).RemoveListener(handler);
        }
    }

    private static ISignal Bind(Type signalType)
    {
        ISignal signal;
        if (signals.TryGetValue(signalType, out signal))
        {
            UnityEngine.Debug.LogError(
                string.Format("Signal already registered for type {0}", signalType.ToString())
            );
            return signal;
        }

        signal = (ISignal)Activator.CreateInstance(signalType);
        signals.Add(signalType, signal);
        return signal;
    }

    private static ISignal Bind<T>()
        where T : ISignal, new()
    {
        return Bind(typeof(T));
    }

    private static ISignal GetSignalByHash(string signalHash)
    {
        foreach (ISignal signal in signals.Values)
        {
            if (signal.Hash == signalHash)
            {
                return signal;
            }
        }

        return null;
    }

#if UNITY_EDITOR
    public static void DumpDebugInfo()
    {
        foreach (var kvp in signals)
        {
            if (kvp.Value is ISignalDebugInfo debugInfo)
            {
                UnityEngine.Debug.Log(
                    $"[Signals] {kvp.Key.Name} has {debugInfo.DebugListenerCount} listeners"
                );
            }
        }
    }
#endif
}

/// <summary>
/// Abstract class for Signals, provides hash by type functionality
/// </summary>
public abstract class ABaseSignal : ISignal
{
    protected string _hash;

    /// <summary>
    /// Unique id for this signal
    /// </summary>
    public string Hash
    {
        get
        {
            if (string.IsNullOrEmpty(_hash))
            {
                _hash = this.GetType().ToString();
            }
            return _hash;
        }
    }
}

/// <summary>
/// Generic base for all signal implementations, parameterized by delegate type.
/// Handles add/remove and debug-time dead-listener pruning.
/// </summary>
/// <typeparam name="TDelegate">Delegate type for listeners (e.g., Action, Action&lt;T&gt;)</typeparam>
public abstract class ASignalBase<TDelegate> : ABaseSignal
#if UNITY_EDITOR
        , ISignalDebugInfo
#endif
    where TDelegate : Delegate
{
    private TDelegate _callback;

    /// <summary>
    /// Adds a listener to this signal.
    /// </summary>
    protected void Add(TDelegate handler)
    {
        if (handler == null)
        {
            return;
        }

#if UNITY_EDITOR
        UnityEngine.Debug.Assert(
            handler
                .Method.GetCustomAttributes(
                    typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute),
                    inherit: false
                )
                .Length == 0,
            "Adding anonymous delegates as Signal callbacks is not supported (you wouldn't be able to unregister them later)."
        );
#endif
        _callback = (TDelegate)Delegate.Combine(_callback, handler);
    }

    /// <summary>
    /// Removes a listener from this signal.
    /// </summary>
    protected void Remove(TDelegate handler)
    {
        if (handler == null)
        {
            return;
        }
        _callback = (TDelegate)Delegate.Remove(_callback, handler);
    }

    /// <summary>
    /// Helper to iterate listeners and optionally prune obviously dead ones in the editor.
    /// The caller supplies an invoker that knows how to call the delegate with the right arguments.
    /// </summary>
    protected void ForEachListener(Action<TDelegate> invoker)
    {
        if (_callback == null)
        {
            return;
        }

#if UNITY_EDITOR
        // Fast path: no pruning, no logging, no strict mode → just invoke directly.
        if (
            !Signals.EnableDebugPruning
            && !Signals.EnableDebugDispatchLogging
            && !Signals.ThrowOnDeadListener
        )
        {
            invoker(_callback);
            return;
        }

        var list = _callback.GetInvocationList();
        TDelegate kept = null;

        for (int i = 0; i < list.Length; i++)
        {
            var d = list[i];

            bool isDead = false;
            var target = d.Target as UnityEngine.Object;

            // Dead-listener detection is active if either pruning OR strict mode is enabled.
            if (
                (Signals.EnableDebugPruning || Signals.ThrowOnDeadListener)
                && target != null
                && target == null
            )
            {
                isDead = true;
            }

            if (isDead)
            {
                var msg =
                    $"[Signals] Dead listener on {Hash}: {d.Method.DeclaringType}.{d.Method.Name}. "
                    + "Owner probably forgot to UnsubscribeAllSignals().";

                if (Signals.ThrowOnDeadListener)
                {
                    throw new InvalidOperationException(msg);
                }
                else
                {
                    UnityEngine.Debug.LogWarning(msg + " Auto-removed.");
                }

                continue;
            }

            var typed = (TDelegate)d;

            if (Signals.EnableDebugDispatchLogging)
            {
                UnityEngine.Debug.Log(
                    $"[Signals] Dispatch {Hash} -> #{i}: {d.Method.DeclaringType}.{d.Method.Name} (target={d.Target})"
                );
            }

            invoker(typed);
            kept = kept == null ? typed : (TDelegate)Delegate.Combine(kept, typed);
        }

        _callback = kept;
#else
        invoker(_callback);
#endif
    }

    /// <summary>
    /// Returns true if there is at least one listener registered.
    /// </summary>
    protected bool HasListeners => _callback != null;

#if UNITY_EDITOR
    // Implement ISignalDebugInfo
    int ISignalDebugInfo.DebugListenerCount => _callback?.GetInvocationList().Length ?? 0;
#endif
}

/// <summary>
/// Strongly typed messages with no parameters
/// </summary>
public abstract class ASignal : ASignalBase<Action>
{
    /// <summary>
    /// Adds a listener with an explicit owner that is responsible for cleanup.
    /// Recommended API.
    /// </summary>
    public void AddListener(ISignalListener owner, Action handler)
    {
        if (owner == null)
            throw new ArgumentNullException(nameof(owner));
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        Add(handler);
        SignalLifetimeRegistry.Register(owner, () => RemoveListener(handler));
    }

    /// <summary>
    /// Legacy API: adds a listener without an owner.
    /// Prefer AddListener(ISignalListener, Action) so subscriptions are tracked.
    /// </summary>
    [Obsolete(
        "Use AddListener(ISignalListener owner, Action handler) instead so subscriptions are tracked and cleaned up.",
        false
    )]
    public void AddListener(Action handler)
    {
        Add(handler);
    }

    /// <summary>
    /// Removes a listener from this Signal
    /// </summary>
    public void RemoveListener(Action handler)
    {
        Remove(handler);
    }

    /// <summary>
    /// Dispatch this signal
    /// </summary>
    public void Dispatch()
    {
        ForEachListener(d => d());
    }
}

/// <summary>
/// Strongly typed messages with 1 parameter
/// </summary>
/// <typeparam name="T">Parameter type</typeparam>
public abstract class ASignal<T> : ASignalBase<Action<T>>
{
    /// <summary>
    /// Adds a listener with an explicit owner that is responsible for cleanup.
    /// </summary>
    public void AddListener(ISignalListener owner, Action<T> handler)
    {
        if (owner == null)
            throw new ArgumentNullException(nameof(owner));
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        Add(handler);
        SignalLifetimeRegistry.Register(owner, () => RemoveListener(handler));
    }

    [Obsolete(
        "Use AddListener(ISignalListener owner, Action<T> handler) instead so subscriptions are tracked and cleaned up.",
        false
    )]
    public void AddListener(Action<T> handler)
    {
        Add(handler);
    }

    public void RemoveListener(Action<T> handler)
    {
        Remove(handler);
    }

    /// <summary>
    /// Dispatch this signal with 1 parameter
    /// </summary>
    public void Dispatch(T arg1)
    {
        ForEachListener(d => d(arg1));
    }
}

/// <summary>
/// Strongly typed messages with 2 parameters
/// </summary>
/// <typeparam name="T">First parameter type</typeparam>
/// <typeparam name="U">Second parameter type</typeparam>
public abstract class ASignal<T, U> : ASignalBase<Action<T, U>>
{
    public void AddListener(ISignalListener owner, Action<T, U> handler)
    {
        if (owner == null)
            throw new ArgumentNullException(nameof(owner));
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        Add(handler);
        SignalLifetimeRegistry.Register(owner, () => RemoveListener(handler));
    }

    [Obsolete(
        "Use AddListener(ISignalListener owner, Action<T, U> handler) instead so subscriptions are tracked and cleaned up.",
        false
    )]
    public void AddListener(Action<T, U> handler)
    {
        Add(handler);
    }

    public void RemoveListener(Action<T, U> handler)
    {
        Remove(handler);
    }

    public void Dispatch(T arg1, U arg2)
    {
        ForEachListener(d => d(arg1, arg2));
    }
}

/// <summary>
/// Strongly typed messages with 3 parameters
/// </summary>
/// <typeparam name="T">First parameter type</typeparam>
/// <typeparam name="U">Second parameter type</typeparam>
/// <typeparam name="V">Third parameter type</typeparam>
public abstract class ASignal<T, U, V> : ASignalBase<Action<T, U, V>>
{
    public void AddListener(ISignalListener owner, Action<T, U, V> handler)
    {
        if (owner == null)
            throw new ArgumentNullException(nameof(owner));
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        Add(handler);
        SignalLifetimeRegistry.Register(owner, () => RemoveListener(handler));
    }

    [Obsolete(
        "Use AddListener(ISignalListener owner, Action<T, U, V> handler) instead so subscriptions are tracked and cleaned up.",
        false
    )]
    public void AddListener(Action<T, U, V> handler)
    {
        Add(handler);
    }

    public void RemoveListener(Action<T, U, V> handler)
    {
        Remove(handler);
    }

    public void Dispatch(T arg1, U arg2, V arg3)
    {
        ForEachListener(d => d(arg1, arg2, arg3));
    }
}
