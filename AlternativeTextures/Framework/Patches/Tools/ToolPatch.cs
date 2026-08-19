using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AlternativeTextures.Framework.Models;
using AlternativeTextures.Framework.Patches.StandardObjects;
using AlternativeTextures.Framework.UI;
using AlternativeTextures.Framework.Utilities;
using ConsoleLog;
using DralGeometry;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using static AlternativeTextures.Framework.Models.AlternativeTextureModel;
using Object = StardewValley.Object;

namespace AlternativeTextures.Framework.Patches.Tools;

internal class ToolPatch : PatchTemplate
{
    private readonly Type _object = typeof(Tool);

    internal ToolPatch(IMonitor modMonitor, IModHelper modHelper)
        : base(modMonitor, modHelper) { }

    internal void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(Item), nameof(GenericTool.canBeTrashed), null),
            postfix: new HarmonyMethod(GetType(), nameof(CanBeTrashedPostfix))
        );
        // harmony.Patch(
        //     AccessTools.Method(
        //         _object,
        //         nameof(Tool.drawInMenu),
        //         new[]
        //         {
        //             typeof(SpriteBatch),
        //             typeof(Vector2),
        //             typeof(float),
        //             typeof(float),
        //             typeof(float),
        //             typeof(StackDrawType),
        //             typeof(Color),
        //             typeof(bool),
        //         }
        //     ),
        //     prefix: new HarmonyMethod(GetType(), nameof(DrawInMenuPrefix))
        // );
        harmony.Patch(
            AccessTools.Method(
                _object,
                nameof(GenericTool.beginUsing),
                [typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer)]
            ),
            prefix: new HarmonyMethod(GetType(), nameof(BeginUsingPrefix))
        );
    }

    /// Tools are by default not trashable, this is hardcoded.
    /// So we need to manually override that...
    [HarmonyPriority(Priority.Last)]
    private static void CanBeTrashedPostfix(Item __instance, ref bool __result)
    {
        if (
            __instance is StardewValley.Tools.GenericTool tool
            && (
                tool.QualifiedItemId == AlternativeTextures.TOOL_ID_PAINT_BUCKET
                || tool.QualifiedItemId == AlternativeTextures.TOOL_ID_SCISSORS
                || tool.QualifiedItemId == AlternativeTextures.PAINT_BRUSH_FILLED_ID
                || tool.QualifiedItemId == AlternativeTextures.PAINT_BRUSH_EMPTY_ID
                || tool.QualifiedItemId == AlternativeTextures.TOOL_ID_SPRAY_CAN
                || tool.QualifiedItemId == AlternativeTextures.TOOL_ID_CATALOGUE
            )
        )
        {
            __result = true;
        }
    }

    // private static bool DrawInMenuPrefix(
    //     Tool __instance,
    //     SpriteBatch spriteBatch,
    //     Vector2 location,
    //     ref float scaleSize,
    //     float transparency,
    //     float layerDepth,
    //     StackDrawType drawStackNumber,
    //     Color color,
    //     bool drawShadow
    // )
    // {
    //     // Paint brush requires special draw prefix for
    //     if (__instance.modData.ContainsKey(AlternativeTextures.PAINT_BRUSH_FLAG))
    //     {
    //         var scale = __instance.modData.ContainsKey(AlternativeTextures.PAINT_BRUSH_SCALE)
    //             ? float.Parse(__instance.modData[AlternativeTextures.PAINT_BRUSH_SCALE])
    //             : 0f;
    //         var texture = Managers.ToolManager.GetPaintBrushEmptyTexture();
    //         if (!String.IsNullOrEmpty(__instance.modData[AlternativeTextures.PAINT_BRUSH_FLAG]))
    //         {
    //             texture = Managers.ToolManager.GetPaintBrushFilledTexture();
    //         }
    //         spriteBatch.Draw(
    //             texture,
    //             location + new Vector2(32f, 32f),
    //             new Rectangle(0, 0, 16, 16),
    //             color * transparency,
    //             0f,
    //             new Vector2(8f, 8f),
    //             4f * (scaleSize + scale),
    //             SpriteEffects.None,
    //             layerDepth
    //         );

    //         if (scale > 0f)
    //         {
    //             __instance.modData[AlternativeTextures.PAINT_BRUSH_SCALE] = (scale -= 0.01f).ToString();
    //         }
    //         return false;
    //     }

    //     return true;
    // }

    private static bool BeginUsingPrefix(
        GenericTool __instance,
        ref bool __result,
        GameLocation location,
        int x,
        int y,
        Farmer who
    )
    {
        if (who != Game1.player)
        {
            return true;
        }

        if (__instance.QualifiedItemId == AlternativeTextures.TOOL_ID_PAINT_BUCKET)
        {
            __result = true;
            return UsePaintBucket(location, x, y, who);
        }

        if (__instance.QualifiedItemId == AlternativeTextures.TOOL_ID_SCISSORS)
        {
            __result = true;
            return UseScissors(location, x, y, who);
        }

        if (__instance.QualifiedItemId == AlternativeTextures.TOOL_ID_SPRAY_CAN)
        {
            __result = true;
            return CancelUsing(who);
        }

        if (
            __instance.QualifiedItemId is AlternativeTextures.PAINT_BRUSH_FILLED_ID
            or AlternativeTextures.PAINT_BRUSH_EMPTY_ID
        )
        {
            __result = true;
            return CancelUsing(who);
        }

        if (__instance.QualifiedItemId == AlternativeTextures.TOOL_ID_CATALOGUE)
        {
            __result = true;
            return CancelUsing(who);
        }

        return true;
    }

    static QualifiedIdFor(ModelIdentifier modelIdentifier)
    {
        Dictionary<string, string> nameToIdMap = new Dictionary<string, string>();
        foreach (var kvp in Game1.objectData)
        {
            var unqualifiedId = kvp.Key; // e.g., "128"
            var internalName = kvp.Value.Name; // e.g., "Pufferfish"

            // Avoid crashing if two mods accidentally use the same name
            if (!nameToIdMap.ContainsKey(internalName))
            {
                nameToIdMap[internalName] = $"(O){unqualifiedId}";
            }
        }
    }

    static GridSize gridSizeFor(IPaintable paintable)
    {
        var _sourceRect = paintable.PreviewTexture(paintable.Texture ?? TextureIdentifier.Default)?.SourceRect;
        var sourceRect = _sourceRect ?? new Rectangle(0, 0, 0, 0);

        // var sourceRect = SourceRects.GetSourceRectangle(availableModels.First(), target, availableModels.First().TextureWidth, availableModels.First().TextureHeight, -1);
        switch (paintable.ModelIdentifier)
        {
            case { Type: TextureType.Craftable }:
                if (sourceRect.Height <= 16)
                {
                    return new(rows: 4, columns: 6);
                }
                else
                {
                    return new(rows: 3, columns: 6);
                }

            case { Type: TextureType.Furniture }:
                if (sourceRect.Height >= 64)
                {
                    return new(rows: 2, columns: 6);
                }
                else if (sourceRect.Height >= 32)
                {
                    return new(rows: 3, columns: 6);
                }
                else
                {
                    return new(rows: 4, columns: 6);
                }

            case { Type: TextureType.Flooring }:
                return new(rows: 4, columns: 6);

            case { Type: TextureType.Character }:
                return new(rows: 4, columns: 6);

            case { Type: TextureType.Tree }:
                return new(rows: 2, columns: 6);

            case { Type: TextureType.FruitTree }:
                return new(rows: 1, columns: 3);

            case { Type: TextureType.Crop }:
                return new(rows: 4, columns: 1);

            case { Type: TextureType.GiantCrop }:
                return new(rows: 2, columns: 3);

            case { Type: TextureType.Grass }:
                return new(rows: 6, columns: 4);

            case { Type: TextureType.Bush }:
                return new(rows: 6, columns: 4);

            case { Type: TextureType.Building }:
                return new(rows: 1, columns: 3);
            case { Type: TextureType.Decoration, Name: "Floor" }:
                return new(rows: 3, columns: 4);
            case { Type: TextureType.Decoration, Name: "Wallpaper" }:
                return new(rows: 2, columns: 6);
            default:
                return new(rows: 4, columns: 6);
        }
    }

    internal static bool UsePaintBucket(GameLocation location, int x, int y, Farmer who, bool isSprayCan = false)
    {
        var tile = new Tile(x / 64, y / 64);
        var paintables = IPaintable.OnTile(tile).ToList();

        /// If you stand before a wall with something on it and you try to use this,
        /// it will only find the wall, not the item.
        /// So a small fix to check on tile above if standing in front of a wall:
        if (paintables.Count == 0)
        {
            if (location is DecoratableLocation loc && loc.GetWallpaperID(tile.X, tile.Y) is { } roomId)
            {
                paintables = [.. IPaintable.OnTile(tile with { Y = tile.Y - 1 })];
            }
        }

        if (paintables.FirstOrDefault() is { } paintable)
        {
            List<TextureInfo> items =
            [
                .. PaintBucketMenuData.VanillaTexturesFor(paintable.ModelIdentifier),
                .. PaintBucketMenuData.GetTexturesFor(paintable.ModelIdentifier),
            ];

            if (items.Count == 1)
            {
                // Game1.addHUDMessage(new HUDMessage(_helper.Translation.Get("messages.warning.no_textures_for_season", new { itemName = modelName }), 3));
                // csharpier-ignore
                Game1.addHUDMessage(new HUDMessage($"No alternative textures found for {paintable.ModelIdentifier.Name} ({paintable.ModelIdentifier.Type})"));
                return CancelUsing(who);
            }

            var gridMenu = new GridMenu(
                [.. items
                    .Select(textureInfo =>
                        (GridMenu.Item)
                            new TextureGridMenuItem()
                            {
                                Paintable = paintable,
                                TextureIdentifier = textureInfo.TextureIdentifier,
                                DisplayName = textureInfo.DisplayName,
                            }
                    )],
                gridSizeFor(paintable),
                uiTitle: _helper.Translation.Get("tools.name.paint_bucket"),
                onPress: (item) =>
                {
                    if (item is TextureGridMenuItem betterItem)
                    {
                        betterItem.Paintable.ApplyTexture(betterItem.TextureIdentifier);
                    }
                }
            );

            Game1.activeClickableMenu = gridMenu;
            if (paintable.Texture is { } texture)
            {
                gridMenu.ScrollTo(items.FindIndex(x => x.TextureIdentifier == texture));
            }

            return CancelUsing(who);
        }

        ////////////////////////////////////////////////////

        if (location is DecoratableLocation decoratableLocation)
        {
            if (decoratableLocation.GetWallpaperID(tile.X, tile.Y) is { } wallId)
            {
                var wallpaperPaintable = new WallpaperDecorationPaintable(decoratableLocation, wallId);

                List<TextureInfo> items =
                [
                    .. PaintBucketMenuData.VanillaWallpaperDecorations(),
                    .. PaintBucketMenuData.GetTexturesFor(wallpaperPaintable.ModelIdentifier),
                ];
                if (items.Count == 1)
                {
                    // csharpier-ignore
                    Game1.addHUDMessage(new HUDMessage($"No alternative textures found for {wallpaperPaintable.ModelIdentifier.Name} ({wallpaperPaintable.ModelIdentifier.Type})"));
                    return CancelUsing(who);
                }

                var gridMenu = new GridMenu(
                    [
                        .. items.Select(textureInfo => new TextureGridMenuItem()
                        {
                            DisplayName = textureInfo.DisplayName,
                            TextureIdentifier = textureInfo.TextureIdentifier,
                            Paintable = wallpaperPaintable,
                        }),
                    ],
                    new(rows: 2, columns: 6),
                    uiTitle: _helper.Translation.Get("tools.name.paint_bucket"),
                    onPress: (item) =>
                    {
                        if (item is TextureGridMenuItem betterItem)
                        {
                            betterItem.Paintable.ApplyTexture(betterItem.TextureIdentifier);
                        }
                    }
                );

                Game1.activeClickableMenu = gridMenu;
                if (wallpaperPaintable.Texture is { } texture)
                {
                    gridMenu.ScrollTo(items.FindIndex(x => x.TextureIdentifier == texture));
                }

                return CancelUsing(who);
            }

            if (decoratableLocation.GetFloorID(tile.X, tile.Y) is { } floorId)
            {
                var floorPaintable = new FloorDecorationPaintable(decoratableLocation, floorId);

                List<TextureInfo> items =
                [
                    .. PaintBucketMenuData.VanillaFloorDecorations(),
                    .. PaintBucketMenuData.GetTexturesFor(floorPaintable.ModelIdentifier),
                ];
                if (items.Count == 1)
                {
                    // csharpier-ignore
                    Game1.addHUDMessage(new HUDMessage($"No alternative textures found for {floorPaintable.ModelIdentifier.Name} ({floorPaintable.ModelIdentifier.Type})"));
                    return CancelUsing(who);
                }

                var gridMenu = new GridMenu(
                    [
                        .. items.Select(textureInfo => new TextureGridMenuItem()
                        {
                            DisplayName = textureInfo.DisplayName,
                            TextureIdentifier = textureInfo.TextureIdentifier,
                            Paintable = floorPaintable,
                        }),
                    ],
                    new(rows: 3, columns: 4),
                    uiTitle: _helper.Translation.Get("tools.name.paint_bucket"),
                    onPress: (item) =>
                    {
                        if (item is TextureGridMenuItem betterItem)
                        {
                            betterItem.Paintable.ApplyTexture(betterItem.TextureIdentifier);
                        }
                    }
                );

                Game1.activeClickableMenu = gridMenu;
                if (floorPaintable.Texture is { } texture)
                {
                    gridMenu.ScrollTo(items.FindIndex(x => x.TextureIdentifier == texture));
                }

                return CancelUsing(who);
            }
        }

        ////////////////////////////////////////////////

        // if (location.IsBuildableLocation() && isSprayCan is false)
        // {
        //     var targetedBuilding = location.getBuildingAt(new Vector2(x / 64, y / 64));
        //     bool isFarmerHouse = false;

        //     if (location is Farm farm)
        //     {
        //         var farmerHouse = farm.GetMainFarmHouse();

        //         // Check for mailbox
        //         var mailboxPosition = farm.GetMainMailboxPosition();
        //         if (PatchTemplate.IsPositionNearMailbox(location, mailboxPosition, x / 64, y / 64))
        //         {
        //             var modelType = AlternativeTextureModel.TextureType.Building;
        //             if (!location.modData.ContainsKey("AlternativeTextureName.Mailbox") || !location.modData["AlternativeTextureName.Mailbox"].Contains("Mailbox"))
        //             {
        //                 var textureModel = new AlternativeTextureModel() { Owner = AlternativeTextures.DEFAULT_OWNER, Season = Game1.GetSeasonForLocation(Game1.currentLocation).ToString() };

        //                 location.modData["AlternativeTextureOwner.Mailbox"] = textureModel.Owner;
        //                 location.modData["AlternativeTextureName.Mailbox"] = String.Concat(textureModel.Owner, ".", $"{modelType}_{"Mailbox"}_{Game1.GetSeasonForLocation(Game1.currentLocation)}");

        //                 if (!String.IsNullOrEmpty(textureModel.Season))
        //                 {
        //                     location.modData["AlternativeTextureSeason.Mailbox"] = Game1.GetSeasonForLocation(Game1.currentLocation).ToString();
        //                 }

        //                 location.modData["AlternativeTextureVariation.Mailbox"] = "-1";
        //             }

        //             bool usedSecondaryTile = string.IsNullOrEmpty(location.doesTileHaveProperty(x / 64, y / 64, "Action", "Buildings")) && location.doesTileHaveProperty(x / 64, (y + 64) / 64, "Action", "Buildings") == "Mailbox";
        //             var mailboxObj = new Object("100", 1, isRecipe: false, -1)
        //             {
        //                 TileLocation = new Vector2(x / 64, (y + (usedSecondaryTile ? 64 : 0)) / 64)
        //             };

        //             foreach (string key in location.modData.Keys)
        //             {
        //                 mailboxObj.modData[key] = location.modData[key];
        //             }

        //             var modelName = mailboxObj.modData["AlternativeTextureName.Mailbox"].Replace($"{mailboxObj.modData["AlternativeTextureOwner.Mailbox"]}.", String.Empty);
        //             if (mailboxObj.modData.ContainsKey("AlternativeTextureSeason.Mailbox") && !String.IsNullOrEmpty(mailboxObj.modData["AlternativeTextureSeason.Mailbox"]))
        //             {
        //                 modelName = GetModelNameWithoutSeason(modelName, mailboxObj.modData["AlternativeTextureSeason.Mailbox"]);
        //             }

        //             if (AlternativeTextures.textureManager.GetAvailableTextureModels(modelName, Game1.GetSeasonForLocation(Game1.currentLocation)).Count == 0)
        //             {
        //                 Game1.addHUDMessage(new HUDMessage(_helper.Translation.Get("messages.warning.no_textures_for_season", new { itemName = modelName }), 3));
        //                 return CancelUsing(who);
        //             }

        //             // Display texture menu
        //             Game1.activeClickableMenu = new PaintBucketMenu(mailboxObj, mailboxObj.TileLocation * 64f, TextureType.Craftable, modelName, _helper.Translation.Get("tools.name.paint_bucket"), isSprayCan: false);

        //             return CancelUsing(who);
        //         }

        //         if (farmerHouse == targetedBuilding)
        //         {
        //             isFarmerHouse = true;

        //             targetedBuilding = new Building();
        //             targetedBuilding.buildingType.Value = $"Farmhouse_{Game1.MasterPlayer.HouseUpgradeLevel}";
        //             targetedBuilding.tileX.Value = farmerHouse.tileX.Value;
        //             targetedBuilding.tileY.Value = farmerHouse.tileY.Value;
        //             targetedBuilding.tilesWide.Value = farmerHouse.tilesWide.Value;
        //             targetedBuilding.tilesHigh.Value = farmerHouse.tilesHigh.Value;

        //             var modelType = AlternativeTextureModel.TextureType.Building;
        //             if (!farm.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) || !farm.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME].Contains(targetedBuilding.buildingType.Value))
        //             {
        //                 var instanceSeasonName = $"{modelType}_{targetedBuilding.buildingType.Value}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
        //                 AssignDefaultModData(farm, instanceSeasonName, true);
        //             }

        //             foreach (string key in farm.modData.Keys)
        //             {
        //                 targetedBuilding.modData[key] = farm.modData[key];
        //             }
        //         }
        //     }

        //     if (targetedBuilding != null)
        //     {
        //         // Assign default data if none exists
        //         if (!targetedBuilding.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME))
        //         {
        //             var modelType = AlternativeTextureModel.TextureType.Building;
        //             var instanceSeasonName = $"{modelType}_{targetedBuilding.buildingType.Value}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
        //             AssignDefaultModData(targetedBuilding, instanceSeasonName, true);
        //         }

        //         var modelName = targetedBuilding.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME].Replace($"{targetedBuilding.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER]}.", String.Empty);
        //         if (targetedBuilding.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_SEASON) && !String.IsNullOrEmpty(targetedBuilding.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]))
        //         {
        //             modelName = GetModelNameWithoutSeason(modelName, targetedBuilding.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]);
        //         }

        //         if (AlternativeTextures.textureManager.GetAvailableTextureModels(modelName, Game1.GetSeasonForLocation(Game1.currentLocation)).Count == 0)
        //         {
        //             if (targetedBuilding.GetData() is var data && data is not null && data.Skins is not null && data.Skins.Count > 0)
        //             {
        //                 // Skip no texture warning
        //             }
        //             else
        //             {
        //                 Game1.addHUDMessage(new HUDMessage(_helper.Translation.Get("messages.warning.no_textures_for_season", new { itemName = modelName }), 3));
        //                 return CancelUsing(who);
        //             }
        //         }

        //         // Verify this building has a texture we can target
        //         if (isFarmerHouse is false)
        //         {
        //             var texturePath = PathUtilities.NormalizePath(Path.Combine(targetedBuilding.textureName() + ".png"));
        //             try
        //             {
        //                 _ = _helper.GameContent.Load<Texture2D>(Path.Combine(targetedBuilding.textureName()));
        //                 _monitor.Log($"{modelName} has a targetable texture within Buildings: {texturePath}", LogLevel.Trace);
        //             }
        //             catch (ContentLoadException ex)
        //             {
        //                 Game1.addHUDMessage(new HUDMessage(AlternativeTextures.modHelper.Translation.Get("messages.warning.custom_building_not_supported", new { itemName = modelName }), 3));
        //                 _monitor.Log($"Failed to load texture for {modelName} at the path {texturePath}: {ex}", LogLevel.Trace);
        //                 return CancelUsing(who);
        //             }
        //         }

        //         // Display texture menu
        //         var buildingObj = new Object(targetedBuilding.buildingType.Value, 1, isRecipe: false, -1)
        //         {
        //             TileLocation = new Vector2(targetedBuilding.tileX.Value, targetedBuilding.tileY.Value)
        //         };
        //         buildingObj.modData.SetFromSerialization(targetedBuilding.modData);

        //         Game1.activeClickableMenu = GetMenu(buildingObj, buildingObj.TileLocation * 64f, GetTextureType(targetedBuilding), modelName, _helper.Translation.Get("tools.name.paint_bucket"), textureTileWidth: targetedBuilding.tilesWide.Value, isSprayCan: isSprayCan);

        //         return CancelUsing(who);
        //     }
        // }

        return CancelUsing(who);
    }

    private static bool UseScissors(GameLocation location, int x, int y, Farmer who)
    {
        var character = GetCharacterAt(location, x, y);
        if (character != null)
        {
            // Assign default data if none exists
            var modelType = AlternativeTextureModel.TextureType.Character;
            if (!character.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME))
            {
                var instanceSeasonName =
                    $"{modelType}_{GetCharacterName(character)}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                AssignDefaultModData(character, instanceSeasonName, true);
            }

            var modelName = character
                .modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME]
                .Replace($"{character.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER]}.", String.Empty);
            if (modelName.Contains(GetCharacterName(character), StringComparison.OrdinalIgnoreCase) is false)
            {
                modelName =
                    $"{modelType}_{GetCharacterName(character)}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
            }

            if (
                character.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_SEASON)
                && !String.IsNullOrEmpty(character.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON])
            )
            {
                modelName = GetModelNameWithoutSeason(
                    modelName,
                    character.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]
                );
            }

            if (
                AlternativeTextures
                    .textureManager.GetAvailableTextureModels(
                        modelName,
                        Game1.GetSeasonForLocation(Game1.currentLocation)
                    )
                    .Count == 0
            )
            {
                if (
                    (
                        character is Pet pet
                        && pet.GetPetData() is var petData
                        && petData is not null
                        && petData.Breeds is not null
                    )
                    || (
                        character is FarmAnimal animal
                        && animal.GetAnimalData() is var animalData
                        && animalData is not null
                        && animalData.Skins is not null
                    )
                )
                {
                    // Skip no texture warning
                }
                else
                {
                    Game1.addHUDMessage(
                        new HUDMessage(
                            _helper.Translation.Get(
                                "messages.warning.no_textures_for_season",
                                new { itemName = modelName }
                            ),
                            3
                        )
                    );
                    return CancelUsing(who);
                }
            }

            // Display texture menu
            var obj = new Object("100", 1, isRecipe: false, -1)
            {
                Name = character.Name,
                displayName = character.displayName,
                TileLocation = character.Tile,
                Location = location,
            };
            obj.modData.SetFromSerialization(character.modData);

            /// TODO
            // Game1.activeClickableMenu = new PaintBucketMenu(obj, obj.TileLocation * 64f, GetTextureType(character), modelName, uiTitle: _helper.Translation.Get("tools.name.scissors"));

            return CancelUsing(who);
        }
        return CancelUsing(who);
    }

    internal static bool UseTextureCatalogue(Farmer who)
    {
        Game1.activeClickableMenu = new CatalogueMenu(who);

        return CancelUsing(who);
    }

    private static bool CancelUsing(Farmer who)
    {
        who.CanMove = true;
        who.UsingTool = false;
        return false;
    }
}
