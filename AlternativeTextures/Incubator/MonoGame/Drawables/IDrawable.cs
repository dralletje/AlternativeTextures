using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Incubator.MonoGame;

public interface IDraw
{
    public void Draw(SpriteBatch spriteBatch, Rectangle destination);
}

static class IDraw_At
{
    extension(IDraw drawable)
    {
        public IDraw At(Vector2 position) => new PositionedDrawable(drawable, position);

        public IDraw At(int x, int y) => new PositionedDrawable(drawable, new Vector2(x, y));

        public IDraw Frame(Rectangle frame) => new FramedDrawable(drawable, frame);

        public IDraw Frame(int x, int y, int width, int height) =>
            new FramedDrawable(drawable, new Rectangle(x, y, width, height));

        public IDraw Frame(int width, int height) => new FramedDrawable(drawable, new Rectangle(0, 0, width, height));

        public IDraw Padding(Padding padding) => new PaddedDrawable(drawable, padding);

        public IDraw Padding(int all) => new PaddedDrawable(drawable, new(all: all));

        public IDraw Padding(int horizontal = 0, int vertical = 0) =>
            new PaddedDrawable(drawable, new(horizontal: horizontal, vertical: vertical));

        public IDraw Padding(int left = 0, int right = 0, int top = 0, int bottom = 0) =>
            new PaddedDrawable(drawable, new(left: left, right: right, top: top, bottom: bottom));
    }
}

public record FramedDrawable(IDraw Drawable, Rectangle Frame) : IDraw
{
    public void Draw(SpriteBatch spriteBatch, Rectangle destination)
    {
        Drawable.Draw(spriteBatch, new(destination.X + Frame.X, destination.Y + Frame.Y, Frame.Width, Frame.Height));
    }
}

public record PaddedDrawable(IDraw Drawable, Padding padding) : IDraw
{
    public void Draw(SpriteBatch spriteBatch, Rectangle destination)
    {
        Drawable.Draw(spriteBatch, destination - padding);
    }
}

public record PositionedDrawable(IDraw Drawable, Vector2 Position) : IDraw
{
    public void Draw(SpriteBatch spriteBatch, Rectangle destination)
    {
        Drawable.Draw(
            spriteBatch,
            new((int)Position.X + destination.X, (int)Position.Y + destination.Y, destination.Width, destination.Height)
        );
    }
}

// [CollectionBuilder(typeof(DrawableGroup), nameof(DrawableGroup.Create))]
public record DrawableGroup(List<IDraw> Drawables) : IDraw
{
    // public static DrawableGroup Create(ReadOnlySpan<IDraw> values) => new(values);

    public DrawableGroup(IEnumerable<IDraw> values)
        : this([.. values]) { }

    public DrawableGroup(Action<DrawableBuilder> buildFn)
        : this([
            Function.Run(() =>
            {
                var builder = new DrawableBuilder();
                buildFn(builder);
                return new DrawableGroup(builder.Finish());
            }),
        ]) { }

    public void Draw(SpriteBatch spriteBatch, Rectangle destination)
    {
        foreach (var drawable in Drawables)
        {
            drawable.Draw(spriteBatch, destination);
        }
    }
}

public interface IDrawableBuilder
{
    public void Add(IDraw drawable);

    public void operator +=(IDraw drawable) => Add(drawable);

    public void operator +=(Action<DrawableBuilder> buildFn) => Add(new DrawableGroup(buildFn));
}

// public class SubGroupBuilder(IDrawableBuilder parent, Func<IDraw, IDraw> propsFn) : IDrawableBuilder, IDisposable
// {
//     List<IDraw> Drawables = [];

//     public void Add(IDraw drawable)
//     {
//         Drawables.Add(drawable);
//     }

//     public void operator +=(IDraw drawable) => Add(drawable);

//     public void operator +=(Action<DrawableBuilder> buildFn) => Add(new DrawableGroup(buildFn));

//     public void Dispose()
//     {
//         parent.Add(propsFn(new DrawableGroup(Drawables)));
//     }
// }

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

    public IDisposable Group(Func<DrawableGroup, IDraw>? propsFn = null)
    {
        if (DrawablesStack.Count is 0)
            throw new ArgumentException("DrawableBuilder is dead!!");

        DrawablesStack.Push((drawables: [], finisher: propsFn ?? (x => x)));

        // return new SubGroupBuilder(this, propsFn);
        return new ActionDisposable(() =>
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
