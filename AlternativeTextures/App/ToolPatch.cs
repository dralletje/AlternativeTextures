using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;

namespace AlternativeTextures.App;

internal class ToolPatch(IModHelper _helper)
{
    private readonly Type _object = typeof(Tool);

    internal void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(Item), nameof(GenericTool.canBeTrashed), null),
            postfix: new HarmonyMethod(this.GetType(), nameof(CanBeTrashedPostfix))
        );
        harmony.Patch(
            AccessTools.Method(
                _object,
                nameof(GenericTool.beginUsing),
                [typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer)]
            ),
            prefix: new HarmonyMethod(this.GetType(), nameof(BeginUsingPrefix))
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
                tool.QualifiedItemId
                is AlternativeTextures.TOOL_ID_PAINT_BUCKET
                    or AlternativeTextures.TOOL_ID_SCISSORS
                    or AlternativeTextures.PAINT_BRUSH_FILLED_ID
                    or AlternativeTextures.PAINT_BRUSH_EMPTY_ID
                    or AlternativeTextures.TOOL_ID_SPRAY_CAN
                    or AlternativeTextures.TOOL_ID_CATALOGUE
            )
        )
        {
            __result = true;
        }
    }

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

        if (__instance.QualifiedItemId is AlternativeTextures.TOOL_ID_PAINT_BUCKET)
        {
            __result = true;
            return UsePaintBucket(location, x, y, who);
        }

        if (__instance.QualifiedItemId is AlternativeTextures.TOOL_ID_SCISSORS)
        {
            __result = true;
            return UseScissors(location, x, y, who);
        }

        if (__instance.QualifiedItemId is AlternativeTextures.TOOL_ID_SPRAY_CAN)
        {
            __result = true;
            return CancelUsing(who);
        }

        if (
            __instance.QualifiedItemId
            is AlternativeTextures.PAINT_BRUSH_FILLED_ID
                or AlternativeTextures.PAINT_BRUSH_EMPTY_ID
        )
        {
            __result = true;
            return CancelUsing(who);
        }

        if (__instance.QualifiedItemId is AlternativeTextures.TOOL_ID_CATALOGUE)
        {
            __result = true;
            return CancelUsing(who);
        }

        return true;
    }

    internal static bool UsePaintBucket(GameLocation location, int x, int y, Farmer who, bool isSprayCan = false)
    {
        ////////////////////////////////////////////////

        return CancelUsing(who);
    }

    private static bool UseScissors(GameLocation location, int x, int y, Farmer who)
    {
        // var character = GetCharacterAt(location, x, y);
        // if (character != null)
        // {
        //     // Assign default data if none exists
        //     var modelType = TextureType.Character;
        //     // if (!character.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME))
        //     // {
        //     //     var instanceSeasonName =
        //     //         $"{modelType}_{GetCharacterName(character)}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
        //     //     AssignDefaultModData(character, instanceSeasonName, true);
        //     // }

        //     var modelName = character
        //         .modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME]
        //         .Replace($"{character.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER]}.", String.Empty);
        //     if (modelName.Contains(GetCharacterName(character), StringComparison.OrdinalIgnoreCase) is false)
        //     {
        //         modelName =
        //             $"{modelType}_{GetCharacterName(character)}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
        //     }

        //     if (
        //         character.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_SEASON)
        //         && !String.IsNullOrEmpty(character.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON])
        //     )
        //     {
        //         modelName = GetModelNameWithoutSeason(
        //             modelName,
        //             character.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]
        //         );
        //     }

        //     if (
        //         AlternativeTextures
        //             .textureManager.GetAvailableTextureModels(
        //                 modelName,
        //                 Game1.GetSeasonForLocation(Game1.currentLocation)
        //             )
        //             .Count == 0
        //     )
        //     {
        //         if (
        //             (
        //                 character is Pet pet
        //                 && pet.GetPetData() is var petData
        //                 && petData is not null
        //                 && petData.Breeds is not null
        //             )
        //             || (
        //                 character is FarmAnimal animal
        //                 && animal.GetAnimalData() is var animalData
        //                 && animalData is not null
        //                 && animalData.Skins is not null
        //             )
        //         )
        //         {
        //             // Skip no texture warning
        //         }
        //         else
        //         {
        //             Game1.addHUDMessage(
        //                 new HUDMessage(
        //                     AlternativeTextures.modHelper.Translation.Get(
        //                         "messages.warning.no_textures_for_season",
        //                         new { itemName = modelName }
        //                     ),
        //                     3
        //                 )
        //             );
        //             return CancelUsing(who);
        //         }
        //     }

        //     // Display texture menu
        //     var obj = new Object("100", 1, isRecipe: false, -1)
        //     {
        //         Name = character.Name,
        //         displayName = character.displayName,
        //         TileLocation = character.Tile,
        //         Location = location,
        //     };
        //     obj.modData.SetFromSerialization(character.modData);

        //     /// TODO
        //     // Game1.activeClickableMenu = new PaintBucketMenu(obj, obj.TileLocation * 64f, GetTextureType(character), modelName, uiTitle: _helper.Translation.Get("tools.name.scissors"));

        //     return CancelUsing(who);
        // }
        return CancelUsing(who);
    }

    internal static bool UseTextureCatalogue(Farmer who)
    {
        /// IMMEDIATE TODO
        // Game1.activeClickableMenu = new CatalogueMenu(who);

        return CancelUsing(who);
    }

    private static bool CancelUsing(Farmer who)
    {
        who.CanMove = true;
        who.UsingTool = false;
        return false;
    }
}
