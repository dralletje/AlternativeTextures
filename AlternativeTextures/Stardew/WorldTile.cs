using Microsoft.Xna.Framework;
using StardewValley;

namespace AlternativeTextures.Stardew;

public record WorldTile(GameLocation Location, int X, int Y) : Tile(X, Y)
{
    public WorldTile(GameLocation location, Vector2 vector)
        : this(location, (int)vector.X, (int)vector.Y) { }

    public Tile Tile => new(this.X, this.Y);

    public new Vector2 ToVector2()
    {
        return new Vector2(this.X, this.Y);
    }

    /// TODO Change this to return `Point`, as it doesn't really make sense to multiple a tile
    // public static Tile operator *(Tile p, int factor) => new(p.X * factor, p.Y * factor);

    public static WorldTile operator +(WorldTile a, Tile b) => new(a.Location, a.X + b.X, a.Y + b.Y);

    public static WorldTile operator -(WorldTile a, Tile b) => new(a.Location, a.X - b.X, a.Y - b.Y);

    public static WorldTile operator -(WorldTile a) => new(a.Location, -a.X, -a.Y);
}
