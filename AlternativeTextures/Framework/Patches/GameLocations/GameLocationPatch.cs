using System;
using System.Linq;
using AlternativeTextures.Framework.Models;
using AlternativeTextures.Framework.Utilities;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;

namespace AlternativeTextures.Framework.Patches.GameLocations;

internal class GameLocationPatch(IMonitor modMonitor, IModHelper modHelper) : PatchTemplate()
{
    private readonly Type _object = typeof(GameLocation);

    internal void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(
                _object,
                nameof(GameLocation.checkAction),
                [typeof(xTile.Dimensions.Location), typeof(xTile.Dimensions.Rectangle), typeof(Farmer)]
            ),
            prefix: new HarmonyMethod(GetType(), nameof(CheckActionPrefix))
        );
        harmony.Patch(
            AccessTools.Method(
                _object,
                nameof(GameLocation.LowPriorityLeftClick),
                [typeof(int), typeof(int), typeof(Farmer)]
            ),
            prefix: new HarmonyMethod(GetType(), nameof(LowPriorityLeftClickPrefix))
        );
        harmony.Patch(
            AccessTools.Method(_object, nameof(GameLocation.leftClick), [typeof(int), typeof(int), typeof(Farmer)]),
            prefix: new HarmonyMethod(GetType(), nameof(LowPriorityLeftClickPrefix))
        );
        harmony.Patch(
            AccessTools.Method(_object, nameof(GameLocation.seasonUpdate), [typeof(bool)]),
            postfix: new HarmonyMethod(GetType(), nameof(SeasonUpdatePostfix))
        );
    }

    private static bool CheckActionPrefix(
        GameLocation __instance,
        ref bool __result,
        xTile.Dimensions.Location tileLocation,
        xTile.Dimensions.Rectangle viewport,
        Farmer who
    )
    {
        if (Game1.didPlayerJustRightClick())
        {
            return true;
        }

        if (
            who.CurrentTool is GenericTool tool
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
            var position =
                (!Game1.wasMouseVisibleThisFrame)
                    ? Game1.player.GetToolLocation()
                    : new Vector2(Game1.getOldMouseX() + Game1.viewport.X, Game1.getOldMouseY() + Game1.viewport.Y);
            tool.beginUsing(__instance, (int)position.X, (int)position.Y, who);
            __result = false;
            return false;
        }

        return true;
    }

    private static bool LowPriorityLeftClickPrefix(GameLocation __instance, ref bool __result, int x, int y, Farmer who)
    {
        if (
            who.CurrentTool is GenericTool tool
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
            __result = false;
            return false;
        }

        return true;
    }

    internal static void SeasonUpdatePostfix(GameLocation __instance, bool onLoad = false)
    {
        /// Can this even happen??
        if (__instance is null)
            return;

        var season = __instance.GetSeason();

        if (__instance.objects is not null)
        {
            foreach (var obj in __instance.objects.Values)
            {
                if (
                    GetTextureForUse(
                        obj.modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_NAME),
                        obj.modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION)
                    ) is
                    { } textureModel
                )
                {
                    if (textureModel.Season != season)
                    {
                        obj.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = textureModel
                            .UniqueIdentifierWithoutSeason.WithSeason(season)
                            .LegacyId;
                    }
                }
            }
        }

        if (__instance.characters is not null)
        {
            foreach (var character in __instance.characters)
            {
                if (
                    GetTextureForUse(
                        character.modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_NAME),
                        character.modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION)
                    ) is
                    { } textureModel
                )
                {
                    if (textureModel.Season != season)
                    {
                        character.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = textureModel
                            .UniqueIdentifierWithoutSeason.WithSeason(season)
                            .LegacyId;
                    }
                }
            }
        }

        if (__instance.animals is not null)
        {
            foreach (var animal in __instance.animals.Values)
            {
                if (
                    GetTextureForUse(
                        animal.modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_NAME),
                        animal.modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION)
                    ) is
                    { } textureModel
                )
                {
                    if (textureModel.Season != season)
                    {
                        animal.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = textureModel
                            .UniqueIdentifierWithoutSeason.WithSeason(season)
                            .LegacyId;
                    }
                }
            }
        }

        if (
            __instance.IsBuildableLocation()
            && UniqueTextureIdentifier.FromString(
                __instance.modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_NAME),
                __instance.modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION)
            )
                is { } textureIdentifier
        )
        {
            var buildingType = $"Farmhouse_{Game1.MasterPlayer.HouseUpgradeLevel}";
            if (textureIdentifier.Season != season)
            {
                __instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = (
                    textureIdentifier with
                    {
                        Season = season,
                    }
                ).LegacyId;
            }
        }
    }
}
