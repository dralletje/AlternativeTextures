using System;
using System.Collections.Generic;
using AlternativeTextures.Framework.Patches;
using AlternativeTextures.Framework.Patches.StandardObjects;
using AlternativeTextures.Stardew;
using ConsoleLog;
using Incubator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.GameData.FloorsAndPaths;
using StardewValley.Mods;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace AlternativeTextures.Framework;

using TextureType = TextureType;

public static class EnumUtil
{
    public static TEnum? ParseOrNull<TEnum>(string value, bool ignoreCase = false)
        where TEnum : struct, Enum
    {
        return Enum.TryParse(value, ignoreCase, out TEnum result) ? result : null;
    }
}

static class Function
{
    public static T Tap<T>(Func<T> function)
    {
        return function();
    }

    public static T Run<T>(Func<T> function)
    {
        return function();
    }
}

interface IPaintable
{
    ModelIdentifier ModelIdentifier { get; }
    TextureIdentifierWithoutSeason? TextureIdentifier { get; }

    /// Instead of storing Related, make this `Draw` (+ `DrawInMenu`?) so this is actually an abstraction
    object? Related { get; }

    public void ApplyTexture(TextureIdentifierWithoutSeason? texture);
    public DrawableTexture? PreviewTexture(UniqueTextureIdentifier texture);

    public DrawableTexture? PreviewTexture(TextureIdentifierWithoutSeason texture, Season season) =>
        PreviewTexture(texture.WithSeason(season));

    /// Currently this is called `Type`, but I rather switch it to `id`, or something more sensible
    string Type
    {
        get { return $"{Category}_{InstanceName}"; }
    }
    public TextureType Category
    {
        get { return this.ModelIdentifier.Type; }
    }
    public string InstanceName
    {
        get { return this.ModelIdentifier.String; }
    }

    private static string HoeDirtName(HoeDirt hoeDirt)
    {
        return Game1.objectData.ContainsKey(hoeDirt.crop.netSeedIndex.Value)
            ? Game1.objectData[hoeDirt.crop.netSeedIndex.Value].Name
            : string.Empty;
    }

    static IEnumerable<IPaintable> OnTile(Tile tile)
    {
        var location = Game1.currentLocation;

        var pixel = tile * Game1.tileSize;
        var xTile = pixel.X;
        var yTile = pixel.Y;

        foreach (var placedObject in location.GetObjectsAtTile(tile))
        {
            var modelType = placedObject is Furniture ? TextureType.Furniture : TextureType.Craftable;
            yield return new PaintableFromModData(placedObject.modData)
            {
                ModelIdentifier = modelType.WithName(PatchTemplate.GetObjectName(placedObject)),
                // ItemId = placedObject.ItemId,
                Related = placedObject,
            };
        }

        IPaintable? maybeResourceClumpPaintable = PatchTemplate.GetResourceClumpAt(location, xTile, yTile) switch
        {
            GiantCrop giantCrop => new PaintableFromModData(giantCrop.modData)
            {
                ModelIdentifier = TextureType.GiantCrop.WithName(giantCrop.InternalName!),
                Related = giantCrop,
            },

            ResourceClump clump => new PaintableFromModData(clump.modData)
            {
                ModelIdentifier = TextureType.ResourceClump.WithName(ResourceClumpPatch.GetResourceClumpName(clump)!),
                Related = clump,
            },

            /// DRAL TODO Distinguish between terrainFeature != null and no terrain at all?
            // { } unknown => null,
            null => null,
        };
        if (maybeResourceClumpPaintable is { } resourceClumpPaintable)
        {
            yield return resourceClumpPaintable;
        }

        IPaintable? maybeTerrainFeaturePaintable = location.GetTerrainFeatureAtTile(tile) switch
        {
            Flooring flooring => new PaintableFromModData(flooring.modData)
            {
                ModelIdentifier = TextureType.Flooring.WithName(flooring.InternalName),
                Related = flooring,
            },
            HoeDirt hoeDirt when hoeDirt.crop is not null => new PaintableFromModData(hoeDirt.modData)
            {
                ModelIdentifier = TextureType.Crop.WithName(HoeDirtName(hoeDirt)),
                Related = hoeDirt,
            },
            Grass grass => new PaintableFromModData(grass.modData)
            {
                ModelIdentifier = TextureType.Grass.WithName("Grass"),
                Related = grass,
            },
            Bush bush => new PaintableFromModData(bush.modData)
            {
                ModelIdentifier = TextureType.Bush.WithName(PatchTemplate.GetBushTypeString(bush)),
                Related = bush,
            },
            Tree tree => new PaintableFromModData(tree.modData)
            {
                ModelIdentifier = TextureType.Tree.WithName(PatchTemplate.GetTreeTypeString(tree)),
                Related = tree,
            },
            FruitTree fruitTree => new PaintableFromModData(fruitTree.modData)
            {
                // Dictionary<int, string> data = Game1.content.Load<Dictionary<int, string>>("Data\\fruitTrees");
                // var saplingName = Game1.fruitTreeData.ContainsKey(fruitTree.treeId.Value) ? Game1.objectData[fruitTree.treeId.Value].Name : String.Empty;

                ModelIdentifier = new()
                {
                    Type = TextureType.Tree,
                    String = Game1.fruitTreeData.ContainsKey(fruitTree.treeId.Value)
                        ? Game1.objectData[fruitTree.treeId.Value].Name
                        : string.Empty,
                    IsName = true,
                },
                Related = fruitTree,
            },

            /// DRAL TODO Distinguish between terrainFeature != null and no terrain at all?
            { } unknown => null,
            null => null,
        };
        if (maybeTerrainFeaturePaintable is { } terrainFeaturePaintable)
        {
            yield return terrainFeaturePaintable;
        }
    }
}

static class ModDataToTextureIdentifierWithoutSeason
{
    public static TextureIdentifierWithoutSeason? GetTexture(ModDataDictionary modData)
    {
        return
            modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) is { } name
            && modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION) is { } variation
            ? TextureIdentifierWithoutSeason.FromString(name, variation)
            : null;
    }

    public static void SetTexture(ModDataDictionary modData, TextureIdentifierWithoutSeason? value)
    {
        if (value is { } texture)
        {
            modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = texture.Owner;
            modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = texture.LegacyId;
            modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = texture.Variation.ToString();
        }
        else
        {
            modData.Remove(ModDataKeys.ALTERNATIVE_TEXTURE_OWNER);
            modData.Remove(ModDataKeys.ALTERNATIVE_TEXTURE_NAME);
            modData.Remove(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION);
        }
    }
}

static class TextureHelper
{
    /// TODO: Do this without `related`?
    public static DrawableTexture? GetDefault(ModelIdentifier modelIdentifier, object? related)
    {
        if (related is StardewValley.Object Related)
        {
            if (modelIdentifier.Type is TextureType.Craftable)
            {
                return Related.bigCraftable.Value
                    ? new()
                    {
                        Texture = Game1.bigCraftableSpriteSheet,
                        SourceRect = StardewValley.Object.getSourceRectForBigCraftable(Related.ParentSheetIndex),
                    }
                    : new()
                    {
                        Texture = Game1.objectSpriteSheet,
                        SourceRect = GameLocation.getSourceRectForObject(Related.ParentSheetIndex),
                    };
            }
            else if (Related is Furniture furniture)
            {
                return ItemRegistry.GetData(Related.QualifiedItemId) is { } data
                    ? new()
                    {
                        Texture = data.GetTexture(),
                        // SourceRect = data.GetSourceRect(),
                        SourceRect = furniture.sourceRect.Value,
                    }
                    : null;
            }
            else
            {
                // Console.Log($"Related: {Related.GetType().ToString()}");
                Console.Log($"Related: {Related.ToString()}");
                return null;
            }
        }
        else if (related is TerrainFeature terrainFeature)
        {
            var variation = -1;
            return terrainFeature switch
            {
                Tree tree => new()
                {
                    Texture = tree.texture.Value,
                    SourceRect = SourceRects.GetTreeSourceRect(tree, variation, 0),
                },
                FruitTree fruitTree => new()
                {
                    Texture = fruitTree.texture,
                    SourceRect = SourceRects.GetFruitTreeSourceRect(fruitTree, variation, 0),
                },
                Flooring flooring => Function.Tap<DrawableTexture>(() =>
                {
                    var whichFloor = flooring.whichFloor.Value.ToString();
                    var floorData = Game1.content.Load<Dictionary<string, FloorPathData>>("Data/FloorsAndPaths");
                    var texturePath =
                        Game1.GetSeasonForLocation(flooring.Location) is Season.Winter
                        && (flooring.Location == null || !flooring.Location.isGreenhouse.Value)
                            ? floorData[whichFloor].WinterTexture
                            : floorData[whichFloor].Texture;
                    var texture = Game1.content.Load<Texture2D>(texturePath);
                    return new()
                    {
                        Texture = texture,
                        SourceRect = SourceRects.GetFlooringSourceRect(flooring, variation, 0),
                    };
                }),
                HoeDirt hoeDirt => new()
                {
                    Texture = Game1.cropSpriteSheet,
                    SourceRect = SourceRects.GetCropSourceRect(hoeDirt.crop, variation, 0),
                },
                Grass grass => new()
                {
                    Texture = grass.texture.Value,
                    SourceRect = SourceRects.GetGrassSourceRect(grass, variation, 0),
                },
                Bush bush => new()
                {
                    Texture = Bush.texture.Value,
                    SourceRect = SourceRects.GetBushSourceRect(bush, variation, 0),
                },

                ResourceClump resourceclump => Function.Tap<DrawableTexture>(() =>
                {
                    var texture = resourceclump.textureName.Value is { } textureName
                        ? Game1.content.Load<Texture2D>(textureName)
                        : Game1.objectSpriteSheet;

                    var sourceRectForStandardTileSheet = Game1.getSourceRectForStandardTileSheet(
                        texture,
                        resourceclump.parentSheetIndex.Value,
                        16,
                        16
                    );
                    var sourceRect = sourceRectForStandardTileSheet with
                    {
                        Width = resourceclump.width.Value * 16,
                        Height = resourceclump.height.Value * 16,
                    };

                    return new() { Texture = texture, SourceRect = sourceRect };
                }),

                _ => null,
            };
        }
        else
        {
            return null;
        }
    }
}

record PaintableFromModData(ModDataDictionary modData) : IPaintable
{
    public required ModelIdentifier ModelIdentifier { get; init; }

    public required object? Related { get; init; }

    public TextureIdentifierWithoutSeason? TextureIdentifier
    {
        get { return ModDataToTextureIdentifierWithoutSeason.GetTexture(modData); }
    }

    public void ApplyTexture(TextureIdentifierWithoutSeason? texture)
    {
        ModDataToTextureIdentifierWithoutSeason.SetTexture(modData, texture);
    }

    public DrawableTexture? PreviewTexture(UniqueTextureIdentifier textureIdentifier)
    {
        if (textureIdentifier.IsDefault)
        {
            return TextureHelper.GetDefault(ModelIdentifier, Related);
        }
        else
        {
            var textureModel = AlternativeTextures.textureManager.GetTexture(textureIdentifier);
            if (textureModel is null)
                /// Texture not found (most likely mod removed),
                /// fall back to default
                return TextureHelper.GetDefault(ModelIdentifier, this.Related);

            if (this.Related is StardewValley.Object Related)
            {
                return new(
                    parent: textureModel.Texture,
                    sourceRect: SourceRects.GetSourceRectangle(
                        Related,
                        textureModel.Variation,
                        textureModel.TextureWidth,
                        textureModel.TextureHeight
                    )
                );
            }
            else
            {
                return this.Related is TerrainFeature terrainFeature
                    ? terrainFeature switch
                    {
                        Tree tree => textureModel.Texture.WithSourceRect(
                            SourceRects.GetTreeSourceRect(tree, textureModel.Variation, textureModel.TextureHeight)
                        ),
                        FruitTree fruitTree => textureModel.Texture.WithSourceRect(
                            SourceRects.GetFruitTreeSourceRect(
                                fruitTree,
                                textureModel.Variation,
                                textureModel.TextureHeight
                            )
                        ),
                        Flooring flooring => textureModel.Texture.WithSourceRect(
                            SourceRects.GetFlooringSourceRect(
                                flooring,
                                textureModel.Variation,
                                textureModel.TextureHeight
                            )
                        ),
                        HoeDirt hoeDirt => textureModel.Texture.WithSourceRect(
                            SourceRects.GetCropSourceRect(
                                hoeDirt.crop,
                                textureModel.Variation,
                                textureModel.TextureHeight
                            )
                        ),
                        Grass grass => textureModel.Texture.WithSourceRect(
                            SourceRects.GetGrassSourceRect(grass, textureModel.Variation, textureModel.TextureHeight)
                        ),
                        Bush bush => textureModel.Texture.WithSourceRect(
                            SourceRects.GetBushSourceRect(bush, textureModel.Variation, textureModel.TextureHeight)
                        ),
                        ResourceClump clump => textureModel.Texture.WithSourceRect(new Rectangle(0, 0, 32, 32)),
                        _ => null,
                    }
                    : null;
            }
        }

        // if (variation == -1 || option.TextureIdentifier.Owner == AlternativeTextures.DEFAULT_OWNER)
        // {
        //     if (PatchTemplate.IsDGAUsed() && PatchTemplate.IsDGAObject(PatchTemplate.GetObjectAt(Game1.currentLocation, (int)_position.X, (int)_position.Y)))
        //     {
        //         this.availableTextures[i].item.drawInMenu(b, new Vector2(this.availableTextures[i].bounds.X, this.availableTextures[i].bounds.Y + 32f), 2, 1f, 0.87f, StackDrawType.Hide, colorOverlay, false);
        //     }
        //     else if (_textureTarget is Fence)
        //     {
        //         this.availableTextures[i].texture = (_textureTarget as Fence).loadFenceTexture();
        //         this.availableTextures[i].sourceRect = PaintBucketMenu.GetFenceSourceRect(textureModel, _textureTarget as Fence, this.availableTextures[i].sourceRect.Height, -1);
        //         this.availableTextures[i].draw(b, colorOverlay, 0.87f);
        //     }
        //     else if (_textureType is TextureType.Character && PatchTemplate.GetCharacterAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Character character && character != null)
        //     {
        //         character.Sprite.loadedTexture = String.Empty;
        //         if (character is FarmAnimal animal && _skinIdToTextures.ContainsKey(target.modData[_textureNameKey]))
        //         {
        //             this.availableTextures[i].texture = _skinIdToTextures[target.modData[_textureNameKey]];
        //             this.availableTextures[i].sourceRect = character.Sprite.SourceRect;
        //         }
        //         else if (character is Pet pet && _breedIdToTextures.ContainsKey(target.modData[_textureNameKey]))
        //         {
        //             this.availableTextures[i].texture = _breedIdToTextures[target.modData[_textureNameKey]];
        //             this.availableTextures[i].sourceRect = character.Sprite.SourceRect;
        //         }
        //         else
        //         {
        //             this.availableTextures[i].texture = character.Sprite.Texture;
        //             this.availableTextures[i].sourceRect = character.Sprite.SourceRect;
        //         }

        //         this.availableTextures[i].draw(b, colorOverlay, 0.87f);
        //     }
        //     else if (_textureType is TextureType.Craftable && PatchTemplate.GetObjectAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) != null)
        //     {
        //         this.availableTextures[i].texture = _textureTarget.bigCraftable.Value ? Game1.bigCraftableSpriteSheet : Game1.objectSpriteSheet;
        //         this.availableTextures[i].sourceRect = _textureTarget.bigCraftable.Value ? Object.getSourceRectForBigCraftable(_textureTarget.ParentSheetIndex) : GameLocation.getSourceRectForObject(_textureTarget.ParentSheetIndex);
        //         this.availableTextures[i].draw(b, colorOverlay, 0.87f);
        //     }
        //     else if (PatchTemplate.GetObjectAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) != null)
        //     {
        //         this.availableTextures[i].item.drawInMenu(b, new Vector2(this.availableTextures[i].bounds.X, this.availableTextures[i].bounds.Y + 32f), 2, 1f, 0.87f, StackDrawType.Hide, colorOverlay, false);
        //     }
        //     else if (PatchTemplate.GetResourceClumpAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is GiantCrop giantCrop)
        //     {
        //         var moreGiantCropsApi = AlternativeTextures.apiManager.GetMoreGiantCropsApi();
        //         if (moreGiantCropsApi is not null && moreGiantCropsApi.GetTexture(giantCrop.parentSheetIndex.Value) is Texture2D moreGiantCropsTexture)
        //         {
        //             this.availableTextures[i].texture = moreGiantCropsTexture;
        //             this.availableTextures[i].sourceRect = new Rectangle(0, 0, 48, 63);
        //         }
        //         else
        //         {
        //             GiantCropData data = giantCrop.GetData();

        //             this.availableTextures[i].texture = Game1.content.Load<Texture2D>(data.Texture);
        //             this.availableTextures[i].sourceRect = new Rectangle(data.TexturePosition.X, data.TexturePosition.Y, 16 * data.TileSize.X, 16 * (data.TileSize.Y + 1));
        //         }
        //         this.availableTextures[i].draw(b, colorOverlay, 0.87f);
        //     }
        //     else if (Game1.currentLocation is Farm farm && farm.GetMainFarmHouse().occupiesTile(new Vector2(_position.X, _position.Y) / 64))
        //     {
        //         var farmerHouse = farm.GetMainFarmHouse();

        //         Texture2D house_texture = BuildingPainter.Apply(Farm.houseTextures, "Buildings\\houses_PaintMask", farmerHouse.netBuildingPaintColor.Value);
        //         if (house_texture is null)
        //         {
        //             house_texture = Farm.houseTextures;
        //         }

        //         b.Draw(house_texture, new Vector2(this.availableTextures[i].bounds.X, this.availableTextures[i].bounds.Y), BuildingPatch.GetSourceRectReversePatch(farmerHouse), farmerHouse.color, 0f, new Vector2(0f, 0f), _buildingScale, SpriteEffects.None, 0.89f);
        //     }
        //     else if (Game1.currentLocation is Farm mailBoxFarm && mailBoxFarm.GetMainMailboxPosition() is Point mailboxPosition && PatchTemplate.IsPositionNearMailbox(Game1.currentLocation, mailboxPosition, (int)(_position.X / 64), (int)(_position.Y / 64)))
        //     {
        //         Texture2D mailboxTexture = Game1.content.Load<Texture2D>($"Maps\\{Game1.GetSeasonForLocation(Game1.currentLocation).ToString().ToLower()}_outdoorsTileSheet");
        //         b.Draw(mailboxTexture, new Vector2(this.availableTextures[i].bounds.X, this.availableTextures[i].bounds.Y), new Rectangle(80, 1232, 16, 32), Color.White, 0f, new Vector2(0f, 0f), 4f, SpriteEffects.None, 0.89f);
        //     }
        //     else if (PatchTemplate.GetBuildingAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Building building)
        //     {
        //         if (_skinIdToTextures.ContainsKey(target.modData[_textureDisplayNameKey]))
        //         {
        //             this.availableTextures[i].texture = _skinIdToTextures[target.modData[_textureDisplayNameKey]];
        //             building.skinId.Value = target.modData[_textureDisplayNameKey];
        //         }
        //         else
        //         {
        //             building.skinId.Value = null;
        //         }
        //         BuildingPatch.ResetTextureReversePatch(building);
        //         BuildingPatch.CondensedDrawInMenu(building, building.texture.Value, b, this.availableTextures[i].bounds.X, this.availableTextures[i].bounds.Y, _buildingScale);

        //         if (building is ShippingBin shippingBin)
        //         {
        //             b.Draw(Game1.mouseCursors, new Vector2(this.availableTextures[i].bounds.X + 4, this.availableTextures[i].bounds.Y - 20), new Rectangle(134, 226, 30, 25), colorOverlay, 0f, Vector2.Zero, _buildingScale, SpriteEffects.None, 1f);
        //         }
        //     }
        //     else if (Game1.currentLocation is DecoratableLocation decoratableLocation && (string.IsNullOrEmpty(decoratableLocation.GetFloorID((int)_position.X, (int)_position.Y)) is false || string.IsNullOrEmpty(decoratableLocation.GetWallpaperID((int)_position.X, (int)_position.Y)) is false))
        //     {
        //         var which = variation;
        //         var isFloor = _modelName.Contains("Floor");

        //         this.availableTextures[i].texture = Game1.content.Load<Texture2D>("Maps\\walls_and_floors");
        //         this.availableTextures[i].sourceRect = (isFloor ? new Rectangle(which % 8 * 32, 336 + which / 8 * 32, 32, 32) : new Rectangle(which % 16 * 16, which / 16 * 48, 16, 48));
        //         this.availableTextures[i].draw(b, colorOverlay, 0.87f);
        //     }
        // }
        // else if (PatchTemplate.IsDGAUsed() && PatchTemplate.IsDGAObject(PatchTemplate.GetObjectAt(Game1.currentLocation, (int)_position.X, (int)_position.Y)) && PatchTemplate.GetObjectAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Furniture)
        // {
        //     var offset = textureModel.TextureHeight <= 16 ? 32 : 0;
        //     this.availableTextures[i].texture = textureModel.GetTexture(variation);
        //     this.availableTextures[i].sourceRect = GetSourceRectangle(textureModel, _textureTarget, textureModel.TextureWidth, textureModel.TextureHeight, variation);
        //     b.Draw(this.availableTextures[i].texture, new Vector2((float)this.availableTextures[i].bounds.X + (float)(this.availableTextures[i].sourceRect.Width / 2) * this.availableTextures[i].baseScale, (float)this.availableTextures[i].bounds.Y + (float)(this.availableTextures[i].sourceRect.Height / 2) * this.availableTextures[i].baseScale + offset), this.availableTextures[i].sourceRect, colorOverlay, 0f, new Vector2(this.availableTextures[i].sourceRect.Width / 2, this.availableTextures[i].sourceRect.Height / 2), this.availableTextures[i].scale, SpriteEffects.None, 0.87f);
        // }
        // else if (_textureType is TextureType.Character && PatchTemplate.GetCharacterAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Character character && character != null)
        // {
        //     this.availableTextures[i].texture = textureModel.GetTexture(variation);
        //     this.availableTextures[i].sourceRect = GetCharacterSourceRectangle(textureModel, character, textureModel.TextureWidth, textureModel.TextureHeight, variation);
        //     this.availableTextures[i].draw(b, colorOverlay, 0.87f);
        // }
        // else if (PatchTemplate.GetObjectAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Furniture)
        // {
        //     this.availableTextures[i].item.drawInMenu(b, new Vector2(this.availableTextures[i].bounds.X, this.availableTextures[i].bounds.Y + 32f), 2f, 1f, 0.87f, StackDrawType.Hide, colorOverlay, false);
        // }
        // else if (PatchTemplate.GetObjectAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) != null)
        // {
        //     this.availableTextures[i].texture = textureModel.GetTexture(variation);
        //     this.availableTextures[i].sourceRect = GetSourceRectangle(textureModel, _textureTarget, textureModel.TextureWidth, textureModel.TextureHeight, variation);
        //     this.availableTextures[i].draw(b, colorOverlay, 0.87f);
        // }
        // else if (PatchTemplate.GetResourceClumpAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is GiantCrop giantCrop)
        // {
        //     this.availableTextures[i].texture = textureModel.GetTexture(variation);
        //     this.availableTextures[i].sourceRect = new Rectangle(0, textureModel.GetTextureOffset(variation), 48, 63);
        //     this.availableTextures[i].draw(b, colorOverlay, 0.87f);
        // }
        // else if (Game1.currentLocation is Farm farm && farm.GetMainFarmHouse().occupiesTile(new Vector2(_position.X, _position.Y) / 64))
        // {
        //     var farmerHouse = farm.GetMainFarmHouse();
        //     var sourceRectangle = BuildingPatch.GetSourceRectReversePatch(farmerHouse);

        //     b.Draw(BuildingPatch.GetBuildingTextureWithPaint(farmerHouse, textureModel, variation, true), new Vector2(this.availableTextures[i].bounds.X, this.availableTextures[i].bounds.Y), new Rectangle(0, 0, sourceRectangle.Width, sourceRectangle.Height), farmerHouse.color, 0f, new Vector2(0f, 0f), _buildingScale, SpriteEffects.None, 0.89f);
        // }
        // else if (Game1.currentLocation is Farm mailBoxFarm && mailBoxFarm.GetMainMailboxPosition() is Point mailboxPosition && PatchTemplate.IsPositionNearMailbox(Game1.currentLocation, mailboxPosition, (int)(_position.X / 64), (int)(_position.Y / 64)))
        // {
        //     b.Draw(textureModel.GetTexture(variation), new Vector2(this.availableTextures[i].bounds.X, this.availableTextures[i].bounds.Y), new Rectangle(0, textureModel.GetTextureOffset(variation), 16, 32), Color.White, 0f, new Vector2(0f, 0f), 4f, SpriteEffects.None, 0.89f);
        // }
        // else if (PatchTemplate.GetBuildingAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Building building)
        // {
        //     BuildingPatch.CondensedDrawInMenu(building, BuildingPatch.GetBuildingTextureWithPaint(building, textureModel, variation), b, this.availableTextures[i].bounds.X, this.availableTextures[i].bounds.Y, _buildingScale);

        //     if (building is ShippingBin shippingBin)
        //     {
        //         b.Draw(textureModel.GetTexture(variation), new Vector2(this.availableTextures[i].bounds.X + 4, this.availableTextures[i].bounds.Y - 20), new Rectangle(32, textureModel.GetTextureOffset(variation), 30, 25), colorOverlay, 0f, Vector2.Zero, _buildingScale, SpriteEffects.None, 1f);
        //     }
        // }
        // else if (Game1.currentLocation is DecoratableLocation decoratableLocation && (string.IsNullOrEmpty(decoratableLocation.GetFloorID((int)_position.X, (int)_position.Y)) is false || string.IsNullOrEmpty(decoratableLocation.GetWallpaperID((int)_position.X, (int)_position.Y)) is false))
        // {
        //     var isFloor = _modelName.Contains("Floor");
        //     var decorationOffset = isFloor ? 8 : 16;

        //     this.availableTextures[i].texture = textureModel.GetTexture(variation);
        //     this.availableTextures[i].sourceRect = new Rectangle((variation % decorationOffset) * textureModel.TextureWidth, (variation / decorationOffset) * textureModel.TextureHeight, textureModel.TextureWidth, textureModel.TextureHeight);
        //     this.availableTextures[i].draw(b, colorOverlay, 0.87f);
        // }
    }
}
