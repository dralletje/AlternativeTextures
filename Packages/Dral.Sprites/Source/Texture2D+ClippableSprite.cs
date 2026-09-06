using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dral.Sprites;

public static class Texture2D_ClippableSprite
{
    extension(Texture2D texture)
    {
        public Sprite Rotate(float angle, Vector2 origin) =>
            new Sprite(new TextureSprite(texture)).Rotate(angle, origin);

        public ClippableSprite Translate(Vector2 offset) => new ClippableSprite(texture).Translate(offset);

        public ClippableSprite Scale(Vector2 scale, Vector2 origin) =>
            new ClippableSprite(texture).Scale(scale, origin);

        public ClippableSprite MultiplyColor(Color color) => new ClippableSprite(texture).MultiplyColor(color);

        public ClippableSprite AddDepth(float depthOffset) => new ClippableSprite(texture).AddDepth(depthOffset);

        public ClippableSprite Flip(SpriteEffects effect, Vector2 origin) =>
            new ClippableSprite(texture).Flip(effect, origin);

        public ClippableSprite ProjectTo(Rectangle destinationRectangle) =>
            new ClippableSprite(texture).ProjectTo(destinationRectangle);
    }
}
