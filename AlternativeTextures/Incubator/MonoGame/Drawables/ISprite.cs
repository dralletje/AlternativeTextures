using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Incubator.MonoGame;

public interface ISprite : IDraw
{
    public void Draw(
        SpriteBatch spriteBatch,
        Vector2 position,
        Color color,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        SpriteEffects effects,
        float depth
    );

    public int Height { get; }
    public int Width { get; }

    void IDraw.Draw(SpriteBatch spriteBatch, Rectangle destination)
    {
        Draw(
            spriteBatch,
            new(destination.X, destination.Y),
            Color.White,
            0f,
            new(0, 0),
            // Width is 0 || Height is 0 ? new(0, 0) : new(destination.Width / Width, destination.Height / Height),
            new(destination.Width / Width, destination.Height / Height),
            SpriteEffects.None,
            0f
        );
    }
}

static class SpriteBatch_ISprite
{
    extension(SpriteBatch spritebatch)
    {
        /// Has destination position and scale arguments
        public void Draw(
            ISprite drawable,
            Vector2 position,
            Color color,
            float rotation,
            Vector2 origin,
            float scale,
            SpriteEffects effects,
            float layerDepth
        )
        {
            drawable.Draw(
                spritebatch,
                position,
                color,
                rotation,
                origin,
                new Vector2(scale, scale),
                effects,
                layerDepth
            );
        }

        public void Draw(
            ISprite texture,
            Vector2 position,
            Color color,
            float rotation,
            Vector2 origin,
            Vector2 scale,
            SpriteEffects effects,
            float layerDepth
        )
        {
            texture.Draw(spritebatch, position, color, rotation, origin, scale, effects, layerDepth);
        }

        /// Has destinationRectangle, so no scale
        public void Draw(
            ISprite texture,
            Rectangle destinationRectangle,
            Color color,
            float rotation,
            Vector2 origin,
            SpriteEffects effects,
            float layerDepth
        )
        {
            texture.Draw(
                spritebatch,
                new Vector2(destinationRectangle.X, destinationRectangle.Y),
                color,
                rotation,
                origin,
                new Vector2(destinationRectangle.Width / texture.Width, destinationRectangle.Height / texture.Height),
                effects,
                layerDepth
            );
        }

        public void Draw(ISprite texture, Rectangle destinationRectangle, Color? color)
        {
            spritebatch.Draw(
                texture,
                destinationRectangle,
                color ?? Color.White,
                rotation: 0f,
                origin: Vector2.Zero,
                effects: SpriteEffects.None,
                layerDepth: 0f
            );
        }
    }
}
