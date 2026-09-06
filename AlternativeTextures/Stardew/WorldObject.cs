using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Locations;

namespace AlternativeTextures.Stardew;

// public enum DecorationType
// {
//     Floor,
//     Wallpaper,
// }

closed public record WorldObject
{
    // 3. Define the union variants as inner records.
    public record Object(StardewValley.Object @object) : WorldObject;

    public record TerrainFeature(StardewValley.TerrainFeatures.TerrainFeature terrainFeature) : WorldObject;

    public record Building(StardewValley.Buildings.Building building) : WorldObject;

    public record Mailbox(Farm farm) : WorldObject;

    public record Floor(DecoratableLocation Location, string RoomId) : WorldObject;

    public record Wallpaper(DecoratableLocation Location, string RoomId) : WorldObject;
}

static class WorldObjectExtensions
{
    extension(GameLocation location)
    {
        public IEnumerable<WorldObject> GetWorldObjectsAt(Tile tile)
        {
            foreach (var placedObject in location.GetObjectsAtTile(tile))
            {
                yield return new WorldObject.Object(placedObject);
            }

            if (location.GetResourceClumpAt(tile) is { } resourceClump)
            {
                yield return new WorldObject.TerrainFeature(resourceClump);
            }

            if (location.GetTerrainFeatureAtTile(tile) is { } terrainFeature)
            {
                yield return new WorldObject.TerrainFeature(terrainFeature);
            }

            if (location is DecoratableLocation decoratableLocation)
            {
                if (decoratableLocation.GetWallpaperID(tile.X, tile.Y) is { } wallId)
                {
                    yield return new WorldObject.Wallpaper(decoratableLocation, wallId);
                }
                else if (decoratableLocation.GetFloorID(tile.X, tile.Y) is { } floorId)
                {
                    yield return new WorldObject.Floor(decoratableLocation, floorId);
                }
            }

            if (location.getBuildingAt(tile.ToVector2()) is { } building)
            {
                yield return new WorldObject.Building(building);
            }
        }
    }

    extension(WorldTile tile)
    {
        public IEnumerable<WorldObject> GetWorldObjects() => tile.Location.GetWorldObjectsAt(tile.Tile);
    }
}
