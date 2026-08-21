using System;
using AlternativeTextures.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace AlternativeTextures.PatchDrawMod.Patches.StandardObjects;

internal class BushPatch(IMonitor modMonitor, IModHelper modHelper) : PatchTemplate()
{
    private readonly Type _object = typeof(Bush);

    internal void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(_object, nameof(Bush.draw), [typeof(SpriteBatch)]),
            prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix))
        );
        harmony.Patch(
            AccessTools.Method(_object, nameof(Bush.seasonUpdate), [typeof(bool)]),
            postfix: new HarmonyMethod(GetType(), nameof(SeasonUpdatePostfix))
        );
    }

    private static bool DrawPrefix(
        Bush __instance,
        float ___yDrawOffset,
        float ___shakeRotation,
        NetRectangle ___sourceRect,
        SpriteBatch spriteBatch
    )
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
            var tileLocation = __instance.Tile;

            var effectiveSize = getEffectiveSize(__instance.size.Value);
            if (__instance.drawShadow.Value)
            {
                if (effectiveSize > 0)
                {
                    spriteBatch.Draw(
                        Game1.mouseCursors,
                        Game1.GlobalToLocal(
                            Game1.viewport,
                            new Vector2(
                                ((tileLocation.X + ((effectiveSize == 1) ? 0.5f : 1f)) * 64f) - 51f,
                                (tileLocation.Y * 64f) - 16f + ___yDrawOffset
                            )
                        ),
                        Bush.shadowSourceRect,
                        Color.White,
                        0f,
                        Vector2.Zero,
                        4f,
                        __instance.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                        1E-06f
                    );
                }
                else
                {
                    spriteBatch.Draw(
                        Game1.shadowTexture,
                        Game1.GlobalToLocal(
                            Game1.viewport,
                            new Vector2(
                                (tileLocation.X * 64f) + 32f,
                                (tileLocation.Y * 64f) + 64f - 4f + ___yDrawOffset
                            )
                        ),
                        Game1.shadowTexture.Bounds,
                        Color.White,
                        0f,
                        new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y),
                        4f,
                        SpriteEffects.None,
                        1E-06f
                    );
                }
            }

            var textureOffset = textureModel.GetTextureOffset(textureVariation);
            var sourceRect = new Rectangle(
                __instance.tileSheetOffset.Value == 1 && __instance.inBloom() ? 32 : 0,
                textureOffset,
                ___sourceRect.Value.Width,
                ___sourceRect.Value.Height
            );
            if (__instance.size.Value == Bush.greenTeaBush)
            {
                sourceRect = new Rectangle(
                    (Math.Min(2, __instance.getAge() / 10) * 16) + (__instance.tileSheetOffset.Value * 16),
                    textureOffset,
                    16,
                    32
                );
            }
            spriteBatch.Draw(
                textureModel.GetTexture(textureVariation),
                Game1.GlobalToLocal(
                    Game1.viewport,
                    new Vector2(
                        (tileLocation.X * 64f) + (float)((effectiveSize + 1) * 64 / 2),
                        ((tileLocation.Y + 1f) * 64f)
                            - (float)(
                                (
                                    effectiveSize > 0
                                    && (!__instance.townBush.Value || effectiveSize != 1)
                                    && __instance.size.Value != 4
                                )
                                    ? 64
                                    : 0
                            )
                            + ___yDrawOffset
                    )
                ),
                sourceRect,
                Color.White,
                ___shakeRotation,
                new Vector2((effectiveSize + 1) * 16 / 2, 32f),
                4f,
                __instance.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                ((float)(__instance.getBoundingBox().Center.Y + 48) / 10000f) - (tileLocation.X / 1000000f)
            );

            return false;
        }
        return true;
    }

    private static int getEffectiveSize(int size)
    {
        if (size == 3)
        {
            return 0;
        }
        return size == 4 ? 1 : size;
    }

    private static void SeasonUpdatePostfix(Bush __instance, bool onLoad)
    {
        if (
            __instance.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME)
            && __instance.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_SEASON)
            && !String.IsNullOrEmpty(__instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON])
        )
        {
            __instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON] = Game1
                .GetSeasonForLocation(__instance.Location)
                .ToString();
            __instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = String.Concat(
                __instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER],
                ".",
                $"{TextureType.Bush}_{GetBushTypeString(__instance)}_{__instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]}"
            );
        }
    }
}
