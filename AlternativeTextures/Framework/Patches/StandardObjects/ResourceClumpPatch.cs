using System;
using AlternativeTextures.Framework.Models;
using AlternativeTextures.Framework.Utilities;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace AlternativeTextures.Framework.Patches.StandardObjects;

internal class ResourceClumpPatch(IMonitor modMonitor, IModHelper modHelper) : PatchTemplate()
{
    private readonly Type _object = typeof(ResourceClump);

    internal void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(_object, nameof(ResourceClump.draw), [typeof(SpriteBatch)]),
            prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix))
        );
        harmony.Patch(
            AccessTools.Method(typeof(TerrainFeature), nameof(TerrainFeature.seasonUpdate), [typeof(bool)]),
            postfix: new HarmonyMethod(GetType(), nameof(SeasonUpdatePostfix))
        );
    }

    private static bool DrawPrefix(ResourceClump __instance, float ___shakeTimer, SpriteBatch spriteBatch)
    {
        if (__instance.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME))
        {
            var textureModel = AlternativeTextures.textureManager.GetSpecificTextureModel(
                __instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME]
            );
            if (textureModel is null)
            {
                return true;
            }

            var textureVariation = Int32.Parse(__instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION]);
            if (
                textureVariation == -1
                || AlternativeTextures.modConfig.IsTextureVariationDisabled(textureModel.GetId(), textureVariation)
            )
            {
                return true;
            }

            var position = __instance.Tile * 64f;
            if (___shakeTimer > 0f)
            {
                position.X += (float)Math.Sin(Math.PI * 2.0 / (double)___shakeTimer) * 4f;
            }

            var textureOffset = textureModel.GetTextureOffset(textureVariation);
            Rectangle sourceRect = new Rectangle(0, textureOffset, 32, 32);

            spriteBatch.Draw(
                textureModel.GetTexture(textureVariation),
                Game1.GlobalToLocal(Game1.viewport, position),
                sourceRect,
                Color.White,
                0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                ((__instance.Tile.Y + 1f) * 64f / 10000f) + (__instance.Tile.X / 100000f)
            );

            return false;
        }

        return true;
    }

    private static void SeasonUpdatePostfix(TerrainFeature __instance, bool onLoad)
    {
        if (__instance is ResourceClump resourceClump)
        {
            if (
                GetTextureForUse(
                    __instance.modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_NAME),
                    __instance.modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION)
                ) is
                { } textureModel
            )
            {
                var season = Game1.GetSeasonForLocation(__instance.Location);
                if (textureModel.Season != season)
                {
                    __instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = textureModel
                        .UniqueIdentifierWithoutSeason.WithSeason(season)
                        .LegacyId;
                }
            }
        }
    }

    public static string GetResourceClumpName(ResourceClump clump)
    {
        return clump.parentSheetIndex.Value switch
        {
            600 => "Stump",
            602 => "Log",
            622 => "Meteor",
            672 => "Boulder",
            _ => String.Empty,
        };
    }
}
