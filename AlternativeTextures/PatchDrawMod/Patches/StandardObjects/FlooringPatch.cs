using System;
using AlternativeTextures.Framework;
using Force.DeepCloner;
using HarmonyLib;
using Incubator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.FloorsAndPaths;
using StardewValley.TerrainFeatures;

namespace AlternativeTextures.PatchDrawMod.Patches.StandardObjects;

internal class FlooringPatch(IModHelper modHelper) : PatchTemplate()
{
    private readonly Type _object = typeof(Flooring);

    internal void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(Flooring), nameof(Flooring.GetData), []),
            postfix: new HarmonyMethod(GetType(), nameof(GetDataPostfix))
        );
    }

    private static void GetDataPostfix(Flooring __instance, ref FloorPathData __result)
    {
        if (
            GetTextureForUse(
                __instance.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_NAME),
                __instance.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION)
            ) is
            { } textureModel
        )
        {
            // var data = __result.ShallowClone()!;
            var data = new FloorPathData
            {
                Id = __result.Id,
                ItemId = __result.ItemId,

                Texture = textureModel.TexturePath,
                Corner = new Point(0, 0),
                WinterTexture = textureModel.TexturePath,
                WinterCorner = new Point(0, 0),

                PlacementSound = __result.PlacementSound,
                RemovalSound = __result.RemovalSound,
                RemovalDebrisType = __result.RemovalDebrisType,
                FootstepSound = __result.FootstepSound,
                ConnectType = __result.ConnectType,
                ShadowType = __result.ShadowType,
                CornerSize = __result.CornerSize,
                FarmSpeedBuff = __result.FarmSpeedBuff,
            };
            __instance.floorTexture = null;
            __instance.floorTextureWinter = null;
            __result = data;
        }
    }
}
