using System;
using System.Collections.Generic;

namespace Incubator;

public interface IState<T>
{
    public T Value { get; set; }
}

public record MappedState<TContained, TExposed>(
    IState<TContained> container,
    Func<TContained, TExposed> @in,
    Func<TExposed, TContained> @out
) : IState<TExposed>
{
    public TExposed Value
    {
        get => @in(container.Value);
        set => container.Value = @out(value);
    }
}

public record ClampedState(IState<int> Container, int Min, int Max) : IState<int>
{
    public int Value
    {
        get => Math.Clamp(Container.Value, Min, Max);
        set => Container.Value = Math.Clamp(value, Min, Max);
    }
}

public interface ILens<TContained, TExposed>
{
    public TExposed ToExposed(TContained contained);
    public TContained ToContained(TExposed exposed);
}
record LensedState<TContained, TExposed>(
    IState<TContained> container,
    ILens<TContained, TExposed> lens
) : IState<TExposed>
{
    public TExposed Value
    {
        get => lens.ToExposed(container.Value);
        set => container.Value = lens.ToContained(value);
    }
}
static class LensExtensions
{
    extension<TContained>(IState<TContained> state)
    {
        public IState<TExposed> Through<TExposed>(ILens<TContained, TExposed> lens) => new LensedState<TContained, TExposed>(state, lens);
    }
}
public record ClampedLens(IState<int> Container, int Min, int Max) : ILens<int, int>
{
    public int ToExposed(int contained) => Math.Clamp(contained, Min, Max);
    public int ToContained(int exposed) => Math.Clamp(exposed, Min, Max);
}

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

public class State<T>(T initialValue) : ISignal<T>, IState<T>
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

    public static implicit operator T(State<T> w) => w.Value;
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
