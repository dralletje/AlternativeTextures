using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Incubator.MonoGame.FlexibleTextures;

public interface ITexture : IDisposable
{
    public void Draw(
        SpriteBatch spritebatch,
        Vector2 position,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        SpriteEffects effects,
        float layerDepth
    );

    /// Has destinationRectangle, so no scale
    public void Draw(
        SpriteBatch spritebatch,
        Rectangle destinationRectangle,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        SpriteEffects effects,
        float layerDepth
    );

    public int Height { get; }
    public int Width { get; }

    public bool IsDisposed { get; }
}

static class ITextureExtensions
{
    extension(ITexture texture)
    {
        public Texture2D Flatten(GraphicsDevice graphicsDevice) =>
            Renderer.Render(
                graphicsDevice,
                texture.Width,
                texture.Height,
                (batch) =>
                {
                    var rectangle = new Rectangle(0, 0, texture.Width, texture.Height);
                    batch.Begin();
                    batch.Draw(texture, rectangle, rectangle, Color.White);
                    batch.End();
                }
            );
    }
}

static class MonoGameExtensions
{
    extension(SpriteBatch spritebatch)
    {
        /// Has destination position and scale arguments
        public void Draw(
            ITexture texture,
            Vector2 position,
            Rectangle? sourceRectangle,
            Color color,
            float rotation,
            Vector2 origin,
            float scale,
            SpriteEffects effects,
            float layerDepth
        )
        {
            spritebatch.Draw(
                texture,
                position,
                sourceRectangle,
                color,
                rotation,
                origin,
                new Vector2(scale, scale),
                effects,
                layerDepth
            );
        }

        public void Draw(
            ITexture texture,
            Vector2 position,
            Rectangle? sourceRectangle,
            Color color,
            float rotation,
            Vector2 origin,
            Vector2 scale,
            SpriteEffects effects,
            float layerDepth
        )
        {
            texture.Draw(spritebatch, position, sourceRectangle, color, rotation, origin, scale, effects, layerDepth);
        }

        /// Has destinationRectangle, so no scale
        public void Draw(
            ITexture texture,
            Rectangle destinationRectangle,
            Rectangle? sourceRectangle,
            Color color,
            float rotation,
            Vector2 origin,
            SpriteEffects effects,
            float layerDepth
        )
        {
            texture.Draw(
                spritebatch,
                destinationRectangle,
                sourceRectangle,
                color,
                rotation,
                origin,
                effects,
                layerDepth
            );
        }

        public void Draw(ITexture texture, Rectangle destinationRectangle, Rectangle sourceRectangle, Color color)
        {
            spritebatch.Draw(
                texture,
                destinationRectangle,
                sourceRectangle,
                color,
                rotation: 0f,
                origin: Vector2.Zero,
                effects: SpriteEffects.None,
                layerDepth: 0f
            );
        }
    }
}
