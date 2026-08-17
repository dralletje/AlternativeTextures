using Microsoft.Xna.Framework;

namespace AlternativeTextures.Framework
{
    public struct Tile(int x, int y)
    {
        public int X = x;
        public int Y = y;

        public Tile(Vector2 vector)
            : this((int)vector.X, (int)vector.Y) { }

        public readonly Vector2 ToVector2()
        {
            return new Vector2(this.X, this.Y);
        }

        /// TODO Change this to return `Point`, as it doesn't really make sense to multiple a tile
        public static Tile operator *(Tile p, int factor) => new(p.X * factor, p.Y * factor);
    }
}
