using System;
using System.Collections.Generic;

namespace Incubator.MonoGame.Drawables;

public interface IDrawableBuilder
{
    public void Add(IDraw drawable);

    public void operator +=(IDraw drawable) => Add(drawable);

    public void operator +=(Action<DrawableBuilder> buildFn) => Add(new DrawableGroup(buildFn));
}

public readonly ref struct ActionDisposableStruct(Action? a) : IDisposable
{
    public void Dispose()
    {
        if (a is not null)
        {
            a();
        }
    }
}

/// TODO Turns into `ref struct` when I got rid of all lambda based uses
public class DrawableBuilder() : IDrawableBuilder
{
    Stack<(List<IDraw> drawables, Func<DrawableGroup, IDraw> finisher)> DrawablesStack = new([
        (drawables: [], finisher: (x) => x),
    ]);

    public static List<IDraw> Create(Action<DrawableBuilder> buildFn)
    {
        var builder = new DrawableBuilder();
        buildFn(builder);
        var children = builder.Finish();
        return children;
    }

    public void Add(IDraw drawable)
    {
        DrawablesStack.Peek().drawables.Add(drawable);
    }

    public void operator +=(IDraw drawable) => Add(drawable);

    public void operator +=(Action<DrawableBuilder> buildFn) => Add(new DrawableGroup(buildFn));

    public ActionDisposableStruct Group(Func<DrawableGroup, IDraw>? propsFn = null)
    {
        if (DrawablesStack.Count is 0)
            throw new ArgumentException("DrawableBuilder is dead!!");

        DrawablesStack.Push((drawables: [], finisher: propsFn ?? (x => x)));

        // return new SubGroupBuilder(this, propsFn);
        return new ActionDisposableStruct(() =>
        {
            var (drawables, finisher) = DrawablesStack.Pop();
            Add(finisher(new DrawableGroup(drawables)));
        });
    }

    public List<IDraw> Finish()
    {
        var (drawables, finisher) = DrawablesStack.Count switch
        {
            0 => throw new ArgumentException("Drawables stack too empty"),
            > 1 => throw new ArgumentException("Drawables stack too full"),
            < 1 => throw new ArgumentException("Huh?"),
            1 => DrawablesStack.Pop(),
        };
        return drawables;
    }
}
