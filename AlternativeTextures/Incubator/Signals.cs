using System;
using System.Collections.Generic;

namespace AlternativeTextures.Incubator;

public interface ISignalBase
{
    void Subscribe(object owner, Action callback);
    void Unsubscribe(object owner);
}

public interface ISignal<out T> : ISignalBase
{
    T Value { get; }
}

internal static class ReactiveContext
{
    internal static (object Owner, Action Callback, HashSet<ISignalBase> Deps)? Current;

    internal static void Track(ISignalBase signal)
    {
        if (Current is var (owner, callback, deps))
        {
            deps.Add(signal);
            signal.Subscribe(owner, callback);
        }
    }
}

public class Signal<T> : ISignal<T>
{
    private T _value;
    private readonly Dictionary<object, Action> _subscribers = new();

    public Signal(T initialValue) => _value = initialValue;

    public T Value
    {
        get
        {
            ReactiveContext.Track(this);
            return _value;
        }
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value))
                return;
            _value = value;
            foreach (var sub in new List<Action>(_subscribers.Values))
                sub();
        }
    }

    public void Subscribe(object owner, Action cb) => _subscribers[owner] = cb;

    public void Unsubscribe(object owner) => _subscribers.Remove(owner);

    public static implicit operator T(Signal<T> w) => w.Value;
}

public class Computed<T> : ISignal<T>
{
    private readonly Func<T> _compute;
    private readonly Dictionary<object, Action> _subscribers = new();
    private readonly HashSet<ISignalBase> _deps = new();
    private T _value = default!;
    private bool _stale = true;

    public Computed(Func<T> compute) => _compute = compute;

    public T Value
    {
        get
        {
            ReactiveContext.Track(this);
            if (_stale)
            {
                foreach (var dep in _deps)
                    dep.Unsubscribe(this);
                _deps.Clear();

                var prev = ReactiveContext.Current;
                ReactiveContext.Current = (this, MarkStale, _deps);
                _value = _compute();
                ReactiveContext.Current = prev;
                _stale = false;
            }
            return _value;
        }
    }

    private void MarkStale()
    {
        if (_stale)
            return;
        _stale = true;
        foreach (var sub in new List<Action>(_subscribers.Values))
            sub();
    }

    public void Subscribe(object owner, Action cb) => _subscribers[owner] = cb;

    public void Unsubscribe(object owner) => _subscribers.Remove(owner);

    public static implicit operator T(Computed<T> w) => w.Value;
}

public class Watcher<T>
{
    private readonly Func<T> _action;
    private readonly HashSet<ISignalBase> _deps = new();
    public bool HasChanges { get; private set; }
    public T Value;

    public Watcher(Func<T> action)
    {
        _action = action;
        Value = Run();
    }

    public T Run()
    {
        foreach (var dep in _deps)
            dep.Unsubscribe(this);
        _deps.Clear();

        HasChanges = false;
        var prev = ReactiveContext.Current;
        ReactiveContext.Current = (this, () => HasChanges = true, _deps);
        var result = _action();
        ReactiveContext.Current = prev;
        return result;
    }
}

public class Watcher
{
    private readonly Watcher<bool> watcher;
    public bool HasChanges
    {
        get { return watcher.HasChanges; }
    }

    public Watcher(Action action)
    {
        watcher = new(() =>
        {
            action();
            return true;
        });
    }

    public void Run()
    {
        watcher.Run();
    }
}
