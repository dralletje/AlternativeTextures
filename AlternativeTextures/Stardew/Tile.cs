using Microsoft.Xna.Framework;

namespace AlternativeTextures.Stardew;

public record Tile(int X, int Y)
{
    public Tile(Vector2 vector)
        : this((int)vector.X, (int)vector.Y) { }

    public Vector2 ToVector2()
    {
        return new Vector2(this.X, this.Y);
    }

    /// TODO Change this to return `Point`, as it doesn't really make sense to multiple a tile
    public static Tile operator *(Tile p, int factor) => new(p.X * factor, p.Y * factor);

    public static Tile operator +(Tile a, Tile b) => new(a.X + b.X, a.Y + b.Y);

    public static Tile operator -(Tile a, Tile b) => new(a.X - b.X, a.Y - b.Y);

    public static Tile operator -(Tile a) => new(-a.X, -a.Y);
}
