using System;
using System.Collections.Generic;
using AlternativeTextures.Framework.Models;
using AlternativeTextures.Framework.Patches;
using AlternativeTextures.Framework.Patches.StandardObjects;
using AlternativeTextures.Framework.Utilities;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Mods;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace AlternativeTextures.Framework;

using TextureType = AlternativeTextureModel.TextureType;

struct ModelIdentifier()
{
    public required TextureType Type { get; init; }
    public required string Name { get; init; }
    public string? ItemId { get; init; }
}

interface IPaintable
{
    /// Currently this is called `Type`, but I rather switch it to `id`, or something more sensible
    string Type { get { return $"{Category}_{InstanceName}"; } }
    public TextureType Category { get { return this.ModelIdentifier.Type; } }
    public string InstanceName { get { return this.ModelIdentifier.Name; } }

    ModelIdentifier ModelIdentifier { get; }

    object? Related { get; }

    TextureIdentifier? Texture { get; set; }

    private static string HoeDirtName(HoeDirt hoeDirt)
    {
        return Game1.objectData.ContainsKey(hoeDirt.crop.netSeedIndex.Value) ? Game1.objectData[hoeDirt.crop.netSeedIndex.Value].Name : string.Empty;
    }

    static List<IPaintable> OnTile(Tile tile)
    {
        var location = Game1.currentLocation;

        var pixel = tile * Game1.tileSize;
        var xTile = pixel.X;
        var yTile = pixel.Y;

        var paintables = new List<IPaintable>();

        foreach (var placedObject in location.GetObjectsAtTile(tile))
        {
            var modelType = placedObject is Furniture ? TextureType.Furniture : TextureType.Craftable;
            paintables.Add(new PaintableFromModData(placedObject.modData) {
                ModelIdentifier = new()
                {
                    Type = modelType,
                    ItemId = placedObject.ItemId,
                    Name = PatchTemplate.GetObjectName(placedObject),
                },
                Related = placedObject,
            });
        }

        if (PatchTemplate.GetResourceClumpAt(location, xTile, yTile) is GiantCrop giantCrop)
        {
            paintables.Add(new PaintableFromModData(giantCrop.modData) {
                ModelIdentifier = new() {
                    Type = TextureType.GiantCrop,
                    Name = giantCrop.InternalName!,
                },
                Related = giantCrop,
            });
        }

        IPaintable? maybeTerrainFeaturePaintable = location.GetTerrainFeatureAtTile(tile) switch
        {
            Flooring flooring =>
                new PaintableFromModData(flooring.modData) {
                    ModelIdentifier = new() {
                        Type = TextureType.Flooring,
                        Name = flooring.InternalName,
                        /// TODO Should I put ItemId here? 
                        // ItemId = flooring.whichFloor.Value,
                    },
                    Related = flooring,
                },
            HoeDirt hoeDirt when hoeDirt.crop is not null =>
                new PaintableFromModData(hoeDirt.modData) {
                    ModelIdentifier = new() {
                        Type = TextureType.Crop,
                        Name = HoeDirtName(hoeDirt),
                    },
                    Related = hoeDirt,
                },
            Grass grass =>
                new PaintableFromModData(grass.modData) {
                    ModelIdentifier = new() {
                        Type = TextureType.Grass,
                        Name = "Grass",
                    },
                    Related = grass,
                },
            Bush bush =>
                new PaintableFromModData(bush.modData) {
                    ModelIdentifier = new() {
                        Type = TextureType.Grass,
                        Name = PatchTemplate.GetBushTypeString(bush),
                    },
                    Related = bush,
                },

            /// DRAL TODO Distinguish between terrainFeature != null and no terrain at all?
            { } unknown => null,
            null => null,
        };
        if (maybeTerrainFeaturePaintable is { } terrainFeaturePaintable)
        {
            paintables.Add(terrainFeaturePaintable);
        }

        return paintables;
    }
}

readonly struct PaintableFromModData(ModDataDictionary modData) : IPaintable
{
    public required readonly ModelIdentifier ModelIdentifier { get; init; }

    public required readonly object? Related { get; init; }
    

    public readonly TextureIdentifier? Texture
    {
        get
        {
            if (
                modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_OWNER) is { } owner
                && modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) is { } name
                && modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION) is { } variation
            )
            {
                return new TextureIdentifier(owner, name, variation);
            }
            else
            {
                return null;
            }
        }
        set
        {
            if (value is { } texture)
            {
                modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = texture.Owner;
                modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = texture.Name;
                modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = texture.Variation;
            }
            else
            {
                modData.Remove(ModDataKeys.ALTERNATIVE_TEXTURE_OWNER);
                modData.Remove(ModDataKeys.ALTERNATIVE_TEXTURE_NAME);
                modData.Remove(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION);
            }
        }
    }
}

// readonly struct UnknownPaintable() : IPaintable
// {
//     public readonly string Type { get; } = "Unknown";

//     public TextureIdentifier? Texture
//     {
//         get
//         {
//             return null;
//         }
//         readonly set
//         {
//             throw new InvalidOperationException("Unknown Paintable can not set texture");
//         }
//     }
// }