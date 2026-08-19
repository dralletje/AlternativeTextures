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

public class Signal<T>(T initialValue) : ISignal<T>
{
    private readonly Dictionary<object, Action> _subscribers = [];

    public T Value
    {
        get
        {
            ReactiveContext.Track(this);
            return field;
        }
        set
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            field = value;
            foreach (var sub in new List<Action>(_subscribers.Values))
                sub();
        }
    } = initialValue;

    public void Subscribe(object owner, Action cb) => _subscribers[owner] = cb;

    public void Unsubscribe(object owner) => _subscribers.Remove(owner);

    public static implicit operator T(Signal<T> w) => w.Value;
}

public class Computed<T>(Func<T> compute) : ISignal<T>
{
    private readonly Func<T> _compute = compute;
    private readonly Dictionary<object, Action> _subscribers = [];
    private readonly HashSet<ISignalBase> _deps = [];
    private bool _stale = true;

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
                field = _compute();
                ReactiveContext.Current = prev;
                _stale = false;
            }
            return field;
        }
    } = default!;

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
    private readonly HashSet<ISignalBase> _deps = [];
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

public class Watcher(Action action)
{
    private readonly Watcher<bool> watcher = new(() =>
    {
        action();
        return true;
    });
    public bool HasChanges
    {
        get { return watcher.HasChanges; }
    }

    public void Run()
    {
        watcher.Run();
    }
}
