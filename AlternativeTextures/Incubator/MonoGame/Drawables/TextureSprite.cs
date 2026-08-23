using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Incubator.MonoGame.Drawables;

public record TextureSprite(Texture2D Texture, Rectangle SourceRect) : ISprite
{
    public TextureSprite(Texture2D Texture)
        : this(Texture, Texture.Bounds) { }

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
        spriteBatch.Draw(Texture, position, SourceRect, color, rotation, origin, scale, effects, depth);
    }

    public int Height { get; } = SourceRect.Height;
    public int Width { get; } = SourceRect.Width;
}

public static class Texture2D_TextureSprite
{
    extension(Texture2D texture)
    {
        public TextureSprite Clip(Rectangle clipRect) => new(texture, clipRect);
    }
}

public static class TextureSprite_ClippableSprite
{
    extension(TextureSprite texture)
    {
        public ClippableSprite ToClippableSprite() => new ClippableSprite(texture.Texture).Clip(texture.SourceRect);

        public ClippableSprite Clip(Rectangle clipRect) => texture.ToClippableSprite().Clip(clipRect);

        public Sprite Rotate(float angle, Vector2 origin) => texture.ToClippableSprite().Rotate(angle, origin);

        public ClippableSprite Translate(Vector2 offset) => texture.ToClippableSprite().Translate(offset);

        public ClippableSprite Scale(Vector2 scale, Vector2 origin) => texture.ToClippableSprite().Scale(scale, origin);

        public ClippableSprite MultiplyColor(Color color) => texture.ToClippableSprite().MultiplyColor(color);

        public ClippableSprite AddDepth(float depthOffset) => texture.ToClippableSprite().AddDepth(depthOffset);

        public ClippableSprite Flip(SpriteEffects effect, Vector2 origin) =>
            texture.ToClippableSprite().Flip(effect, origin);
    }
}
