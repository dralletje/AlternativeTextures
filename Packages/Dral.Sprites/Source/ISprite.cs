using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dral.Sprites;

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
            new((float)destination.Width / (float)Width, (float)destination.Height / (float)Height),
            SpriteEffects.None,
            0f
        );
    }
}

// public static class ISpriteExtensions
// {
//   extension(ISprite sprite)
//   {
//     /// Has destination position and scale arguments
//     public FitSprite Fit() => new(sprite);
//   }
// }

// public record FitSprite(ISprite sprite) : ISprite
// {
//   public int Height => sprite.Height;
//   public int Width => sprite.Width;

//   public void Draw(
//       SpriteBatch spriteBatch,
//       Vector2 position,
//       Color color,
//       float rotation,
//       Vector2 origin,
//       Vector2 scale,
//       SpriteEffects effects,
//       float depth
//   )
//   {
//     var lowestScale = Math.Min(scale.X, scale.Y);
//     sprite.Draw(spriteBatch, position, color, rotation, origin, new(lowestScale, lowestScale), effects, depth);
//   }
// }

public static class SpriteBatch_ISprite
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
                /// Don't think it should happen often that these are zero...
                /// But I'm at least building in a non-error path
                texture.Width is 0 || texture.Height is 0
                    ? new Vector2(0, 0)
                    : new Vector2((float)destinationRectangle.Width / texture.Width, (float)destinationRectangle.Height / texture.Height),
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
