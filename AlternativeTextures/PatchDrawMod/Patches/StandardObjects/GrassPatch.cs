using System;
using AlternativeTextures.Framework;
using HarmonyLib;
using Incubator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace AlternativeTextures.PatchDrawMod.Patches.StandardObjects;

internal class GrassPatch(IMonitor modMonitor, IModHelper modHelper) : PatchTemplate()
{
    private readonly Type _object = typeof(Grass);
    private const string NAME_PREFIX = "Grass";

    internal void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(_object, nameof(Grass.draw), [typeof(SpriteBatch)]),
            prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix))
        );
        harmony.Patch(
            AccessTools.Method(_object, nameof(Grass.seasonUpdate), [typeof(bool)]),
            postfix: new HarmonyMethod(GetType(), nameof(SeasonUpdatePostfix))
        );
    }

    private static bool DrawPrefix(
        Grass __instance,
        int[] ___offset1,
        int[] ___offset2,
        int[] ___offset3,
        int[] ___offset4,
        int[] ___whichWeed,
        float ___shakeRotation,
        double[] ___shakeRandom,
        bool[] ___flip,
        SpriteBatch spriteBatch
    )
    {
        if (
            __instance.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) is { } name
            && __instance.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION) is { } variation
            && PatchTemplate.GetTextureForUse(name, variation) is { } textureModel
        )
        {
            var tileLocation = __instance.Tile;
            var textureOffset = 0;
            for (var i = 0; i < __instance.numberOfWeeds.Value; i++)
            {
                var pos =
                    (i != 4)
                        ? (
                            (tileLocation * 64f)
                            + new Vector2(
                                (float)((i % 2 * 64 / 2) + (___offset3[i] * 4) - 4) + 30f,
                                (i / 2 * 64 / 2) + (___offset4[i] * 4) + 40
                            )
                        )
                        : (
                            (tileLocation * 64f)
                            + new Vector2((float)(16 + (___offset1[i] * 4) - 4) + 30f, 16 + (___offset2[i] * 4) + 40)
                        );
                spriteBatch.Draw(
                    textureModel.Texture.Texture,
                    Game1.GlobalToLocal(Game1.viewport, pos),
                    new Rectangle(0, textureOffset, 15, 20),
                    Color.White,
                    ___shakeRotation / (float)(___shakeRandom[i] + 1.0),
                    new Vector2(7.5f, 17.5f),
                    4f,
                    ___flip[i] ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    ((pos.Y + 16f - 20f) / 10000f) + (pos.X / 1E+07f)
                );
            }
            return false;
        }
        return true;
    }

    private static void SeasonUpdatePostfix(Grass __instance, bool onLoad)
    {
        if (
            __instance.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) is { } textureName
            && __instance.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION) is { } variation
            && UniqueTextureIdentifier.FromString(textureName, variation) is { } textureIdentifier
        )
        {
            var season = Game1.GetSeasonForLocation(__instance.Location);
            if (textureIdentifier.Season != season)
            {
                __instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = textureIdentifier
                    .WithoutSeason.WithSeason(season)
                    .LegacyId;
            }
        }
    }
}
