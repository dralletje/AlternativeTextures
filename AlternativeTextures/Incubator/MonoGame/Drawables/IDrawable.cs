using System;
using System.Collections.Generic;
using Dunet;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Incubator.MonoGame;

public interface IDraw
{
    public void Draw(SpriteBatch spriteBatch, Rectangle destination);
}

public enum Operator
{
    Plus,
    Minus,
}

[Union]
public partial record Calc
{
    public partial record Px(int pixels);

    public partial record Vw(float viewportWidth);

    public partial record Vh(float viewportHeight);

    public partial record Pc(float percentage);

    public partial record Binary(Calc a, Operator @operator, Calc b);

    ///
    public static Calc operator +(Calc a, Calc b) => new Binary(a, Operator.Plus, b);

    public static Calc operator -(Calc a, Calc b) => new Binary(a, Operator.Minus, b);

    public static Calc operator *(Calc calculation, float f) =>
        calculation switch
        {
            Px(var pixels) => new Px((int)(pixels * f)),
            Vw(var viewportWidth) => new Vw(viewportWidth * f),
            Vh(var viewportHeight) => new Vh(viewportHeight * f),
            Pc(var percentage) => new Pc(percentage * f),
            Binary(var a, var @operator, var b) => new Binary(a * f, @operator, b * f),
        };

    public static Calc operator /(Calc calculation, int f) => calculation * (1 / f);

    public int Calculate(int pc, int vh, int vw) =>
        this switch
        {
            Px(var amount) => amount,
            Vw(var fraction) => (int)(fraction * vw),
            Vh(var fraction) => (int)(fraction * vh),
            Pc(var fraction) => (int)(fraction * pc),
            Binary(var a, var @operator, var b) => ApplyOperator(
                a.Calculate(pc, vh, vw),
                @operator,
                b.Calculate(pc, vh, vw)
            ),
        };

    public static Calc Zero = new Calc.Px(0);

    static int ApplyOperator(int a, Operator @operator, int b) =>
        @operator switch
        {
            Operator.Plus => a + b,
            Operator.Minus => a - b,
        };

    public static implicit operator Calc(int pixels) => new Calc.Px(pixels);
}

public static class CalcExtensions
{
    extension(int amount)
    {
        public Calc Px => new Calc.Px(amount);
    }

    extension(float amount)
    {
        public Calc Pc => new Calc.Pc(amount);
        public Calc Vw => new Calc.Vw(amount);
        public Calc Vh => new Calc.Vh(amount);
    }
}

static class IDraw_At
{
    extension(IDraw drawable)
    {
        public IDraw At(Vector2 position) => drawable.Frame(x: (int)position.X, y: (int)position.Y);

        public IDraw At(Calc x, Calc y) => drawable.Frame(x: x, y: y);

        public IDraw At(int x, int y) => drawable.Frame(x: x, y: y);

        // public IDraw Frame(Rectangle frame) => new FramedDrawable(drawable, frame);

        public IDraw Frame(Calc? x = null, Calc? y = null, Calc? width = null, Calc? height = null) =>
            new FramedDrawable(drawable, X: x, Y: y, Width: width, Height: height);

        public IDraw Frame(Calc? width = null, Calc? height = null) =>
            new FramedDrawable(drawable, Width: width, Height: height);

        public IDraw Padding(LayoutPadding padding) => new PaddedDrawable(drawable, padding);

        public IDraw Padding(Padding padding) =>
            new PaddedDrawable(
                drawable,
                new(left: padding.Left, right: padding.Right, top: padding.Top, bottom: padding.Bottom)
            );

        public IDraw Padding(Calc all) => new PaddedDrawable(drawable, new(all: all ?? Calc.Zero));

        public IDraw Padding(Calc horizontal, Calc vertical) =>
            new PaddedDrawable(drawable, new(horizontal: horizontal, vertical: vertical));

        public IDraw Padding(Calc? left = null, Calc? right = null, Calc? top = null, Calc? bottom = null) =>
            new PaddedDrawable(
                drawable,
                new(
                    left: left ?? Calc.Zero,
                    right: right ?? Calc.Zero,
                    top: top ?? Calc.Zero,
                    bottom: bottom ?? Calc.Zero
                )
            );

        public IDraw Centered(Calc width) => new HorizontalCenteredDrawable(drawable, width);
    }
}

public record FramedDrawable(IDraw Drawable, Calc? X = null, Calc? Y = null, Calc? Width = null, Calc? Height = null)
    : IDraw
{
    public void Draw(SpriteBatch spriteBatch, Rectangle destination)
    {
        var x =
            X?.Calculate(
                pc: destination.Width,
                vw: Game1.graphics.GraphicsDevice.Viewport.Width,
                vh: Game1.graphics.GraphicsDevice.Viewport.Height
            ) ?? 0;
        var y =
            Y?.Calculate(
                pc: destination.Height,
                vw: Game1.graphics.GraphicsDevice.Viewport.Width,
                vh: Game1.graphics.GraphicsDevice.Viewport.Height
            ) ?? 0;

        var width =
            Width?.Calculate(
                pc: destination.Width,
                vw: Game1.graphics.GraphicsDevice.Viewport.Width,
                vh: Game1.graphics.GraphicsDevice.Viewport.Height
            ) ?? destination.Width;
        var height =
            Height?.Calculate(
                pc: destination.Height,
                vw: Game1.graphics.GraphicsDevice.Viewport.Width,
                vh: Game1.graphics.GraphicsDevice.Viewport.Height
            ) ?? destination.Height;
        Drawable.Draw(spriteBatch, new(destination.X + x, destination.Y + y, width, height));
    }
}

public record HorizontalCenteredDrawable(IDraw Drawable, Calc Width) : IDraw
{
    public void Draw(SpriteBatch spriteBatch, Rectangle destination)
    {
        var width = Width.Calculate(
            pc: destination.Width,
            vw: Game1.graphics.GraphicsDevice.Viewport.Width,
            vh: Game1.graphics.GraphicsDevice.Viewport.Height
        );
        var spareSpace = destination.Width - width;
        Drawable.Draw(
            spriteBatch,
            new(destination.X + spareSpace / 2, destination.Y, destination.Width - spareSpace, destination.Height)
        );
    }
}

public record PaddedDrawable(IDraw Drawable, LayoutPadding Padding) : IDraw
{
    public void Draw(SpriteBatch spriteBatch, Rectangle destination)
    {
        var padding = Padding.Calculate(
            pcw: destination.Width,
            pch: destination.Height,
            vw: Game1.graphics.GraphicsDevice.Viewport.Width,
            vh: Game1.graphics.GraphicsDevice.Viewport.Height
        );
        Drawable.Draw(spriteBatch, destination - padding);
    }
}

public record PositionedDrawable(IDraw Drawable, Calc X, Calc Y) : IDraw
{
    public void Draw(SpriteBatch spriteBatch, Rectangle destination)
    {
        var x = X.Calculate(
            pc: destination.Width,
            vw: Game1.graphics.GraphicsDevice.Viewport.Width,
            vh: Game1.graphics.GraphicsDevice.Viewport.Height
        );
        var y = Y.Calculate(
            pc: destination.Height,
            vw: Game1.graphics.GraphicsDevice.Viewport.Width,
            vh: Game1.graphics.GraphicsDevice.Viewport.Height
        );
        Drawable.Draw(
            spriteBatch,
            new((int)x + destination.X, (int)y + destination.Y, destination.Width, destination.Height)
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
        : this(DrawableBuilder.Create(buildFn)) { }

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

///////////////////////////////////////////////

public record LayoutPadding()
{
    public Calc Top { get; init; } = 0;
    public Calc Bottom { get; init; } = 0;
    public Calc Left { get; init; } = 0;
    public Calc Right { get; init; } = 0;

    public LayoutPadding(Calc all)
        : this()
    {
        Top = all;
        Bottom = all;
        Left = all;
        Right = all;
    }

    public LayoutPadding(Calc horizontal, Calc vertical)
        : this()
    {
        Top = horizontal;
        Bottom = horizontal;
        Left = vertical;
        Right = vertical;
    }

    public LayoutPadding(Calc left, Calc right, Calc top, Calc bottom)
        : this()
    {
        Top = top;
        Bottom = bottom;
        Left = left;
        Right = right;
    }

    public static LayoutPadding operator *(LayoutPadding p, int factor) =>
        new()
        {
            Top = p.Top * factor,
            Bottom = p.Bottom * factor,
            Left = p.Left * factor,
            Right = p.Right * factor,
        };

    public static LayoutPadding operator +(LayoutPadding a, LayoutPadding b) =>
        new()
        {
            Top = a.Top + b.Top,
            Bottom = a.Bottom + b.Bottom,
            Left = a.Left + b.Left,
            Right = a.Right + b.Right,
        };

    public static LayoutPadding operator -(LayoutPadding a, LayoutPadding b) => a + -b;

    public static LayoutPadding operator -(LayoutPadding a) => a * -1;

    public Padding Calculate(int pcw, int pch, int vw, int vh) =>
        new()
        {
            Left = Left.Calculate(pc: pcw, vw: vw, vh: vh),
            Right = Right.Calculate(pc: pcw, vw: vw, vh: vh),
            Top = Top.Calculate(pc: pch, vw: vw, vh: vh),
            Bottom = Bottom.Calculate(pc: pch, vw: vw, vh: vh),
        };
}
