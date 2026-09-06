using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dral.Sprites;

public readonly record struct SpritePart(
    ISprite Drawable,
    Vector2 Position,
    float Rotation,
    Vector2 Scale,
    Color Color,
    Vector2 Origin,
    SpriteEffects Effects,
    float Depth
)
{
    public static SpritePart Create(
        ISprite drawable,
        Vector2? position = null,
        float? rotation = null,
        Vector2? scale = null,
        Color? color = null,
        Vector2? origin = null,
        SpriteEffects? effects = null,
        float? depth = null
    )
    {
        return new SpritePart(
            drawable,
            position ?? new(0, 0),
            rotation ?? 0f,
            scale ?? new(1, 1),
            color ?? Color.White,
            origin ?? new(0, 0),
            effects ?? SpriteEffects.None,
            depth ?? 0f
        );
    }
}

public record Sprite(IEnumerable<SpritePart> Parts) : ISprite
{
    public Sprite(ISprite drawable)
        : this(ImmutableArray.Create(SpritePart.Create(drawable))) { }

    public Sprite(IEnumerable<ISprite> values)
        : this(values.Select(v => SpritePart.Create(v))) { }

    ///////////////////////////////////////////////

    public Sprite Translate(Vector2 offset) =>
        new(Parts.Select(p => p with { Position = p.Position + offset }).ToImmutableArray());

    public Sprite Scale(Vector2 scale, Vector2 origin) =>
        new(
            Parts
                .Select(p => p with { Position = ((p.Position - origin) * scale) + origin, Scale = p.Scale * scale })
                .ToImmutableArray()
        );

    public Sprite Rotate(float angle, Vector2 origin)
    {
        var (sin, cos) = MathF.SinCos(angle);
        return new(
            Parts
                .Select(p =>
                    p with
                    {
                        Position = new(
                            ((p.Position.X - origin.X) * cos) - ((p.Position.Y - origin.Y) * sin) + origin.X,
                            ((p.Position.X - origin.X) * sin) + ((p.Position.Y - origin.Y) * cos) + origin.Y
                        ),
                        Rotation = p.Rotation + angle,
                    }
                )
                .ToImmutableArray()
        );
    }

    public Sprite MultiplyColor(Color color) =>
        new(
            Parts.Select(p => p with { Color = new Color(p.Color.ToVector4() * color.ToVector4()) }).ToImmutableArray()
        );

    public Sprite AddDepth(float depthOffset) =>
        new(Parts.Select(p => p with { Depth = p.Depth + depthOffset }).ToImmutableArray());

    public Sprite Flip(SpriteEffects effect, Vector2 origin) =>
        new(
            Parts
                .Select(p =>
                    p with
                    {
                        Effects = p.Effects ^ effect,
                        Position = p.Position with
                        {
                            X = effect.HasFlag(SpriteEffects.FlipHorizontally)
                                ? origin.X - (p.Position.X - origin.X)
                                : p.Position.X,
                            Y = effect.HasFlag(SpriteEffects.FlipVertically)
                                ? origin.Y - (p.Position.Y - origin.Y)
                                : p.Position.Y,
                        },
                    }
                )
                .ToImmutableArray()
        );

    public virtual Rectangle GetBounds()
    {
        if (Parts.Count() == 0)
            return Rectangle.Empty;

        float minX = float.MaxValue,
            minY = float.MaxValue,
            maxX = float.MinValue,
            maxY = float.MinValue;
        foreach (var p in Parts)
        {
            var src = new Rectangle(0, 0, p.Drawable.Width, p.Drawable.Height);
            var left = p.Position.X - (p.Origin.X * p.Scale.X);
            var top = p.Position.Y - (p.Origin.Y * p.Scale.Y);

            minX = MathF.Min(minX, left);
            minY = MathF.Min(minY, top);
            maxX = MathF.Max(maxX, left + (src.Width * p.Scale.X));
            maxY = MathF.Max(maxY, top + (src.Height * p.Scale.Y));
        }
        return new Rectangle(
            (int)MathF.Floor(minX),
            (int)MathF.Floor(minY),
            (int)MathF.Ceiling(maxX - minX),
            (int)MathF.Ceiling(maxY - minY)
        );
    }

    public int Height => GetBounds().Height;
    public int Width => GetBounds().Width;

    public void Draw(
        SpriteBatch spriteBatch,
        Vector2 position,
        Color color,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        SpriteEffects effects,
        float depth
    )
    {
        var flipX = effects.HasFlag(SpriteEffects.FlipHorizontally);
        var flipY = effects.HasFlag(SpriteEffects.FlipVertically);
        var invertRot = flipX ^ flipY;

        var (sinGrp, cosGrp) = MathF.SinCos(rotation);

        foreach (var p in Parts)
        {
            var relPos = p.Position - origin;

            if (flipX)
                relPos.X = -relPos.X;
            if (flipY)
                relPos.Y = -relPos.Y;

            relPos *= scale;

            var rotatedRelPos = relPos;
            if (rotation != 0f)
            {
                rotatedRelPos = new Vector2(
                    (relPos.X * cosGrp) - (relPos.Y * sinGrp),
                    (relPos.X * sinGrp) + (relPos.Y * cosGrp)
                );
            }

            var finalPos = rotatedRelPos + position;
            var finalRot = (invertRot ? -p.Rotation : p.Rotation) + rotation;

            // Origin mirroring
            float srcWidth = p.Drawable.Width;
            float srcHeight = p.Drawable.Height;

            Vector2 finalOrigin = new Vector2(
                flipX ? srcWidth - p.Origin.X : p.Origin.X,
                flipY ? srcHeight - p.Origin.Y : p.Origin.Y
            );

            p.Drawable.Draw(
                spriteBatch,
                finalPos,
                new Color(p.Color.ToVector4() * color.ToVector4()),
                finalRot,
                finalOrigin,
                p.Scale * scale,
                p.Effects ^ effects,
                p.Depth + depth
            );
        }
    }

    // public void Draw(
    //     SpriteBatch spriteBatch,
    //     Rectangle destRect,
    //     Color color,
    //     float rotation,
    //     Vector2 origin,
    //     SpriteEffects effects,
    //     float depth
    // )
    // {
    //     Rectangle bounds = GetBounds();
    //     if (bounds.IsEmpty)
    //         return;

    //     float scaleX = (float)destRect.Width / bounds.Width;
    //     float scaleY = (float)destRect.Height / bounds.Height;

    //     Vector2 adjustedOrigin = origin + new Vector2(bounds.X, bounds.Y);

    //     Draw(
    //         spriteBatch,
    //         new Vector2(destRect.X, destRect.Y),
    //         color,
    //         rotation,
    //         adjustedOrigin,
    //         new Vector2(scaleX, scaleY),
    //         effects,
    //         depth
    //     );
    // }

    public virtual Sprite ProjectTo(Rectangle DestinationRectangle) =>
    this
        .Scale(new((float)DestinationRectangle.Width / (float)Width, (float)DestinationRectangle.Height / (float)Height), new(0, 0))
        // .Scale(new(2, 5), new(0, 0))
        .Translate(new(DestinationRectangle.X, DestinationRectangle.Y))
    ;
}

file static class Helpers
{
    internal static ImmutableArray<SpritePart> Combine(ReadOnlySpan<Sprite> values)
    {
        var size = 0;
        foreach (var x in values)
            size += x.Parts.Count();

        var builder = ImmutableArray.CreateBuilder<SpritePart>(size);

        foreach (var x in values)
            builder.AddRange(x.Parts);

        return builder.MoveToImmutable();
    }

    internal static ImmutableArray<SpritePart> Combine(IEnumerable<Sprite> values)
    {
        var size = 0;
        foreach (var x in values)
            size += x.Parts.Count();

        var builder = ImmutableArray.CreateBuilder<SpritePart>(size);

        foreach (var x in values)
            builder.AddRange(x.Parts);

        return builder.MoveToImmutable();
    }

}
