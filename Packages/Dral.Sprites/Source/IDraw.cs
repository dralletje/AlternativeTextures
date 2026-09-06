using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Dral.Sprites;

namespace Dral.Sprites;

public interface IDraw
{
    public void Draw(SpriteBatch spriteBatch, Rectangle destination);
}

public static class IDraw_At
{
    extension(IDraw drawable)
    {
        public IDraw At(Vector2 position) => drawable.Frame(x: (int)position.X, y: (int)position.Y);


        public IDraw At(int x, int y) => drawable.Frame(x: x, y: y);

        public IDraw Frame(Rectangle frame) => new FramedDrawable(drawable, frame.X, frame.Y, frame.Width, frame.Height);

        public IDraw Frame(int? x = null, int? y = null, int? width = null, int? height = null) =>
            new FramedDrawable(drawable, X: x, Y: y, Width: width, Height: height);

        public IDraw Frame(int? width = null, int? height = null) =>
            new FramedDrawable(drawable, Width: width, Height: height);

        public IDraw Padding(Padding padding) => new PaddedDrawable(drawable, padding);

        public IDraw Padding(int all) => new PaddedDrawable(drawable, new(all: all));

        public IDraw Padding(int horizontal, int vertical) =>
            new PaddedDrawable(drawable, new(horizontal: horizontal, vertical: vertical));

        public IDraw Padding(int? left = null, int? right = null, int? top = null, int? bottom = null) =>
            new PaddedDrawable(
                drawable,
                new(
                    left: left ?? 0,
                    right: right ?? 0,
                    top: top ?? 0,
                    bottom: bottom ?? 0
                )
            );

        public IDraw Centered(int width) => new HorizontalCenteredDrawable(drawable, width);

        public IDraw AbsoluteFrame(Rectangle frame) => new AbsoluteFramedDrawable(drawable, frame);
    }
}

public record AbsoluteFramedDrawable(IDraw Drawable, Rectangle Frame) : IDraw
{
    public void Draw(SpriteBatch spriteBatch, Rectangle destination)
    {
        Drawable.Draw(spriteBatch, Frame);
    }
}

public record FramedDrawable(IDraw Drawable, int? X = null, int? Y = null, int? Width = null, int? Height = null)
    : IDraw
{
    public void Draw(SpriteBatch spriteBatch, Rectangle destination)
    {
        var x = X ?? 0;
        var y = Y ?? 0;
        var width = Width ?? destination.Width;
        var height = Height ?? destination.Height;
        Drawable.Draw(spriteBatch, new(destination.X + x, destination.Y + y, width, height));
    }
}

public record HorizontalCenteredDrawable(IDraw Drawable, int Width) : IDraw
{
    public void Draw(SpriteBatch spriteBatch, Rectangle destination)
    {
        var width = Width;
        var spareSpace = destination.Width - width;
        Drawable.Draw(
            spriteBatch,
            new(destination.X + (spareSpace / 2), destination.Y, destination.Width - spareSpace, destination.Height)
        );
    }
}

// public record CenteredLayout(Calc Width, ) : IGroupLayout
// {
//     public void Draw(SpriteBatch spriteBatch, Rectangle destination)
//     {
//         var width = Width.Calculate(
//             pc: destination.Width,
//             vw: Game1.graphics.GraphicsDevice.Viewport.Width,
//             vh: Game1.graphics.GraphicsDevice.Viewport.Height
//         );
//         Console.Log($"width: {width}");
//         var spareSpace = destination.Width - width;
//         Drawable.Draw(
//             spriteBatch,
//             new(destination.X + (spareSpace / 2), destination.Y, destination.Width - spareSpace, destination.Height)
//         );
//     }
// }

public static class Geometry
{
    public static Rectangle Centered(Rectangle frame, int width)
    {
        var spareSpace = frame.Width - width;
        return new(frame.X + (spareSpace / 2), frame.Y, frame.Width - spareSpace, frame.Height);
    }
}

public record PaddedDrawable(IDraw Drawable, Padding Padding) : IDraw
{
    public void Draw(SpriteBatch spriteBatch, Rectangle destination)
    {
        Drawable.Draw(spriteBatch, destination - Padding);
    }
}

// [CollectionBuilder(typeof(DrawableGroup), nameof(DrawableGroup.Create))]
public record DrawableGroup(IEnumerable<IDraw> Drawables) : IDraw, IEnumerable<IDraw>
{
    // public static DrawableGroup Create(ReadOnlySpan<IDraw> values) => new(values);

    public DrawableGroup()
        : this([]) { }

    // public DrawableGroup(Action<DrawableBuilder> buildFn)
    //     : this(DrawableBuilder.Create(buildFn)) { }

    public void Draw(SpriteBatch spriteBatch, Rectangle destination)
    {
        foreach (var drawable in Drawables)
        {
            drawable.Draw(spriteBatch, destination);
        }
    }

    public IEnumerator<IDraw> GetEnumerator()
    {
        return Drawables.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
