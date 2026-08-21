using System.Collections.Generic;
using System.Linq;
using AlternativeTextures.Framework;
using Incubator;
using StardewValley;
using StardewValley.GameData.GiantCrops;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace AlternativeTextures.Stardew;

public static class StardewWorldExtensions
{
    extension(GameLocation location)
    {
        public TerrainFeature? GetTerrainFeatureAtTile(Tile tile)
        {
            var tileVector = tile.ToVector2();
            if (location.terrainFeatures.GetValueOrNull(tileVector) is { } feature)
            {
                return feature;
            }
            else
            {
                return location.largeTerrainFeatures is { } largeTerrainFeatures
                    ? largeTerrainFeatures.FirstOrDefault(t => t is not null && t.Tile == tileVector)
                    : (TerrainFeature?)null;
            }
        }

        public List<Object> GetObjectsAtTile(Tile tile)
        {
            var objects = new List<Object>();
            var pixelCoordinates = tile * Game1.tileSize;

            // If object is furniture and currently has something on top of it, check that instead
            foreach (var furniture in location.furniture.Where(c => c.heldObject.Value != null))
            {
                if (furniture.boundingBox.Value.Contains(pixelCoordinates.X, pixelCoordinates.Y))
                {
                    objects.Add(furniture.heldObject.Value);
                }
            }

            // Prioritize checking non-rug furniture first
            foreach (var furniture in location.furniture.Where(c => c.furniture_type.Value != Furniture.rug))
            {
                if (furniture.boundingBox.Value.Contains(pixelCoordinates.X, pixelCoordinates.Y))
                {
                    objects.Add(furniture);
                }
            }

            // Replicating GameLocation.getObjectAt, but doing objects before rugs
            // Doing this so the object on top of rugs are given instead of the latter
            // var tile = new Vector2(x / 64, y / 64);
            if (location.objects.GetValueOrDefault(tile.ToVector2()) is { } justObject)
            {
                objects.Add(justObject);
            }

            /// Fallback to Stardew implementation for rugs, and cases we might have missed
            if (location.getObjectAtTile(tile.X, tile.Y) is { } fallbackObject)
            {
                if (!objects.Contains(fallbackObject))
                {
                    objects.Add(fallbackObject);
                }
            }

            return objects;
        }

        public ResourceClump? GetResourceClumpAt(Tile tile) =>
            location.resourceClumps.FirstOrDefault(r => r.occupiesTile(tile.X, tile.Y));
    }

    extension(GiantCrop giantCrop)
    {
        internal string? InternalName
        {
            get
            {
                return giantCrop.GetData() is GiantCropData giantCropData
                    ? ItemRegistry.GetData(giantCropData.FromItemId)?.InternalName ?? string.Empty
                    : null;
            }
        }
    }

    extension(Flooring floor)
    {
        internal string InternalName
        {
            get { return ItemRegistry.GetData(floor.GetData()?.ItemId)?.InternalName ?? string.Empty; }
        }
    }
}
