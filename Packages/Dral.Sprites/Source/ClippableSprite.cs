using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dral.Sprites;

public readonly record struct ClippableSpritePart(
    Texture2D Texture,
    Rectangle? SourceRect,
    Vector2 Position,
    Vector2 Scale,
    Color Color,
    Vector2 Origin,
    SpriteEffects Effects,
    float Depth
)
{
    public static ClippableSpritePart Create(
        Texture2D texture,
        Rectangle? sourceRect = null,
        Vector2? position = null,
        Vector2? scale = null,
        Color? color = null,
        Vector2? origin = null,
        SpriteEffects? effects = null,
        float? depth = null
    )
    {
        return new ClippableSpritePart(
            texture,
            sourceRect ?? texture.Bounds,
            position ?? new(0, 0),
            scale ?? new(1, 1),
            color ?? Color.White,
            origin ?? new(0, 0),
            effects ?? SpriteEffects.None,
            depth ?? 0f
        );
    }
}

// [CollectionBuilder(typeof(ClippableSprite), nameof(ClippableSprite.Create))]
public record ClippableSprite(ImmutableArray<ClippableSpritePart> Parts) : ISprite
{
    public static ClippableSprite Create(ReadOnlySpan<ClippableSprite> values) => new(values);

    public ClippableSprite(ReadOnlySpan<ClippableSprite> values)
        : this(Helpers.Combine(values)) { }

    public ClippableSprite(IEnumerable<ClippableSprite> values)
        : this(Helpers.Combine(values)) { }

    public ClippableSprite(Texture2D texture)
        : this(ImmutableArray.Create(ClippableSpritePart.Create(texture))) { }

    ///////////////////////////////////////////////

    public ClippableSprite Clip(Rectangle clipRect)
    {
        var newParts = ImmutableArray.CreateBuilder<ClippableSpritePart>(Parts.Length);
        foreach (var p in Parts)
        {
            var src = p.SourceRect ?? new Rectangle(0, 0, p.Texture.Width, p.Texture.Height);
            var topLeft = p.Position - (p.Origin * p.Scale);

            var localX = (int)((clipRect.X - topLeft.X) / p.Scale.X) + src.X;
            var localY = (int)((clipRect.Y - topLeft.Y) / p.Scale.Y) + src.Y;
            var localW = (int)(clipRect.Width / p.Scale.X);
            var localH = (int)(clipRect.Height / p.Scale.Y);

            var inter = Rectangle.Intersect(src, new Rectangle(localX, localY, localW, localH));
            if (inter.IsEmpty)
                continue;

            var newPos =
                topLeft
                + new Vector2((inter.X - src.X) * p.Scale.X, (inter.Y - src.Y) * p.Scale.Y)
                + (p.Origin * p.Scale)
                - new Vector2(clipRect.X, clipRect.Y);
            newParts.Add(p with { SourceRect = inter, Position = newPos });
        }
        return new(newParts.ToImmutable());
    }

    /////////////////////////////////////////////////

    public Sprite Rotate(float angle, Vector2 origin) => new Sprite(this).Rotate(angle, origin);

    /////////////////////////////////////////////////

    public ClippableSprite Translate(Vector2 offset) =>
        new(Parts.Select(p => p with { Position = p.Position + offset }).ToImmutableArray());

    public ClippableSprite Scale(Vector2 scale, Vector2 origin) =>
        new(
            Parts
                .Select(p => p with { Position = ((p.Position - origin) * scale) + origin, Scale = p.Scale * scale })
                .ToImmutableArray()
        );

    public ClippableSprite MultiplyColor(Color color) =>
        new(
            Parts.Select(p => p with { Color = new Color(p.Color.ToVector4() * color.ToVector4()) }).ToImmutableArray()
        );

    public ClippableSprite AddDepth(float depthOffset) =>
        new(Parts.Select(p => p with { Depth = p.Depth + depthOffset }).ToImmutableArray());

    public ClippableSprite Flip(SpriteEffects effect, Vector2 origin) =>
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
        if (Parts.Length == 0)
            return Rectangle.Empty;

        float minX = float.MaxValue,
            minY = float.MaxValue,
            maxX = float.MinValue,
            maxY = float.MinValue;
        foreach (var p in Parts)
        {
            var src = p.SourceRect ?? new Rectangle(0, 0, p.Texture.Width, p.Texture.Height);
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
            var finalRot = rotation;

            // Origin mirroring
            float srcWidth = p.SourceRect?.Width ?? p.Texture.Width;
            float srcHeight = p.SourceRect?.Height ?? p.Texture.Height;

            Vector2 finalOrigin = new Vector2(
                flipX ? srcWidth - p.Origin.X : p.Origin.X,
                flipY ? srcHeight - p.Origin.Y : p.Origin.Y
            );

            spriteBatch.Draw(
                p.Texture,
                finalPos,
                p.SourceRect,
                new Color(p.Color.ToVector4() * color.ToVector4()),
                finalRot,
                finalOrigin,
                p.Scale * scale,
                p.Effects ^ effects,
                p.Depth + depth
            );
        }
    }

    public ClippableSprite ProjectTo(Rectangle DestinationRectangle) =>
        this.Scale(new(DestinationRectangle.Width / Width, DestinationRectangle.Height / Height), new(0, 0))
            .Translate(new(DestinationRectangle.X, DestinationRectangle.Y));
}

file static class Helpers
{
    internal static ImmutableArray<ClippableSpritePart> Combine(ReadOnlySpan<ClippableSprite> values)
    {
        var size = 0;
        foreach (var x in values)
            size += x.Parts.Length;

        var builder = ImmutableArray.CreateBuilder<ClippableSpritePart>(size);

        foreach (var x in values)
            builder.AddRange(x.Parts);

        return builder.MoveToImmutable();
    }

    internal static ImmutableArray<ClippableSpritePart> Combine(IEnumerable<ClippableSprite> values)
    {
        var size = 0;
        foreach (var x in values)
            size += x.Parts.Length;

        var builder = ImmutableArray.CreateBuilder<ClippableSpritePart>(size);

        foreach (var x in values)
            builder.AddRange(x.Parts);

        return builder.MoveToImmutable();
    }

}
