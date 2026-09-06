using System;
using System.Collections.Generic;
using AlternativeTextures.App.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Dral.Sprites;

namespace Incubator.MonoGame;

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

public interface ILayout
{
    public void Render(ref DrawableBuilder UI);
}

public interface IGroupLayout : ILayout
{
    public ILayout WithChildren(IEnumerable<ILayout> Children);
}

static class ILayoutExtensions
{
    extension(ILayout component)
    {
        public ILayout Frame(Rectangle frame) => new FramedLayout(component, frame);

        // public ILayout ID()
    }
}

record FramedLayout(ILayout Layout, Rectangle Frame) : ILayout
{
    public void Render(ref DrawableBuilder UI)
    {
        using (UI.Group(Frame))
        {
            Layout.Render(ref UI);
        }
    }
}

// interface ILayoutBuilder
// {
//     public Rectangle RootFrame { get; }
//     public Rectangle Frame { get; }

//     public void operator +=(IDraw drawable);

//     public ActionDisposableStruct Group(Rectangle newFrame);
// }

public record Collector<T>
{
    public List<T> Values = [];

    public void operator +=(T value) => Values.Add(value);
}

/// TODO Turns into `ref struct` when I got rid of all lambda based uses
public ref struct DrawableBuilder(Rectangle rootFrame, SpriteBatch? ImmediatePainter = null)
{
    public record Result
    {
        public required List<IDraw> Drawables { get; init; }
        public required List<Func<Point, bool>> LeftClickHandlers { get; init; }
        public required List<Action<bool>> UpdateHandlers { get; init; }
        public required List<Action<GameTime>> TickHandlers { get; init; }
        public required List<Action<Direction>> MovementKeyHandlers { get; init; }
        public required List<Func<Point, bool>> HoverHandlers { get; init; }
    }

    public Collector<Func<Point, bool>> OnLeftClick = new();
    public Collector<Func<Point, bool>> OnHover = new();
    public Collector<Action<bool>> OnUpdate = new();
    public Collector<Action<GameTime>> OnTick = new();
    public Collector<Action<Direction>> OnMovementKey = new();

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

    public void Add(IDraw drawable)
    {
        DrawablesStack.Peek().drawables.Add(drawable);
        if (ImmediatePainter is not null)
        {
            drawable.Draw(ImmediatePainter, Current.Frame);
        }
    }

    public void operator +=(IDraw drawable) => Add(drawable);

    public void operator +=(ILayout drawable)
    {
        drawable.Render(ref this);
    }

    public ActionDisposableStruct Group(Rectangle newFrame)
    {
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
        });
    }

    public Result Finish()
    {
        var layer = DrawablesStack.Count switch
        {
            < 0 => throw new InvalidOperationException("Huh?"),
            0 => throw new InvalidOperationException("Drawables stack too empty"),
            1 => DrawablesStack.Pop(),
            > 1 => throw new InvalidOperationException("Drawables stack too full"),
        };
        return new Result()
        {
            Drawables = layer.drawables,
            LeftClickHandlers = OnLeftClick.Values,
            UpdateHandlers = OnUpdate.Values,
            TickHandlers = OnTick.Values,
            MovementKeyHandlers = OnMovementKey.Values,
            HoverHandlers = OnHover.Values,
        };
    }
}
