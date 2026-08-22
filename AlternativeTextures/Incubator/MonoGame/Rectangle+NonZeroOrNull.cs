using Microsoft.Xna.Framework;

namespace Incubator.MonoGame;

static class Rectangle_NonZeroOrNull
{
    extension(Rectangle rectangle)
    {
        public Rectangle? NonZeroOrNull() => rectangle == Rectangle.Empty ? null : rectangle;
    }
}
