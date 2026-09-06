using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dral.Sprites;

static class ISprite_Sprite
{
    extension(ISprite drawable)
    {
        public Sprite Rotate(float angle, Vector2 origin) => new Sprite(drawable).Rotate(angle, origin);

        public Sprite Translate(Vector2 offset) => new Sprite(drawable).Translate(offset);

        public Sprite Scale(Vector2 scale, Vector2 origin) => new Sprite(drawable).Scale(scale, origin);

        public Sprite MultiplyColor(Color color) => new Sprite(drawable).MultiplyColor(color);

        public Sprite AddDepth(float depthOffset) => new Sprite(drawable).AddDepth(depthOffset);

        public Sprite Flip(SpriteEffects effect, Vector2 origin) => new Sprite(drawable).Flip(effect, origin);

        public Sprite ProjectTo(Rectangle DestinationRectangle) => new Sprite(drawable).ProjectTo(DestinationRectangle);
    }
}
