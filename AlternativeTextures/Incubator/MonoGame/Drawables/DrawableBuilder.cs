using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Xna.Framework;

namespace Incubator.MonoGame.Drawables;

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
public ref struct DrawableBuilder(Rectangle rootFrame)
{
    /// Until I have everything "ref-ed up", I'm going to save some stuff in a reference type
    record Layer()
    {
        public object Identity { get; } = new();
        public required Rectangle Frame;
        public List<IDraw> drawables = [];
    }

    Stack<Layer> DrawablesStack = new([new Layer() { Frame = rootFrame }]);

    Layer Current =>
        DrawablesStack.TryPeek(out var layer)
            ? layer
            : throw new InvalidOperationException("DrawableBuilder is already finished");

    public Rectangle RootFrame => rootFrame;
    public Rectangle Frame => Current.Frame;

    // public static List<IDraw> Create(Action<DrawableBuilder> buildFn)
    // {
    //     var builder = new DrawableBuilder();
    //     buildFn(builder);
    //     var children = builder.Finish();
    //     return children;
    // }

    public void Add(IDraw drawable)
    {
        DrawablesStack.Peek().drawables.Add(drawable);
    }

    public void operator +=(IDraw drawable) => Add(drawable);

    // public void operator +=(Action<DrawableBuilder> buildFn) => Add(new DrawableGroup(buildFn));

    public ActionDisposableStruct Group(Rectangle newFrame)
    {
        // state.CurrentFrame = new();

        if (DrawablesStack.Count is 0)
            throw new ArgumentException("DrawableBuilder is dead!!");

        var layer = new Layer() { Frame = newFrame };
        DrawablesStack.Push(layer);

        var layerIdentity = layer.Identity;
        var _DrawablesStack = DrawablesStack;
        return new ActionDisposableStruct(() =>
        {
            if (!object.ReferenceEquals(_DrawablesStack.Peek().Identity, layerIdentity))
                throw new InvalidOperationException("Popping another Layer from DrawableBuilder then expected");

            var layer = _DrawablesStack.Pop();
            _DrawablesStack.Peek().drawables.Add(new DrawableGroup(layer.drawables).AbsoluteFrame(layer.Frame));
            // _DrawablesStack.Peek().drawables.AddRange(layer.drawables);
        });
    }

    public List<IDraw> Finish()
    {
        var layer = DrawablesStack.Count switch
        {
            < 0 => throw new InvalidOperationException("Huh?"),
            0 => throw new InvalidOperationException("Drawables stack too empty"),
            1 => DrawablesStack.Pop(),
            > 1 => throw new InvalidOperationException("Drawables stack too full"),
        };
        return layer.drawables;
    }
}
