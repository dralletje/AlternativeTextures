using System;
using AlternativeTextures.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace AlternativeTextures.PatchDrawMod.Patches.StandardObjects;

internal class CropPatch(IModHelper modHelper) : PatchTemplate()
{
    private readonly Type _object = typeof(Crop);

    internal void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(
                _object,
                nameof(Crop.draw),
                [typeof(SpriteBatch), typeof(Vector2), typeof(Color), typeof(float)]
            ),
            prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix))
        );
        harmony.Patch(
            AccessTools.Method(
                _object,
                nameof(Crop.drawWithOffset),
                [typeof(SpriteBatch), typeof(Vector2), typeof(Color), typeof(float), typeof(Vector2)]
            ),
            prefix: new HarmonyMethod(GetType(), nameof(DrawWithOffsetPrefix))
        );
    }

    [HarmonyBefore(["spacechase0.DynamicGameAssets"])]
    private static bool DrawPrefix(
        Crop __instance,
        Vector2 ___origin,
        Vector2 ___drawPosition,
        Rectangle ___sourceRect,
        Rectangle ___coloredSourceRect,
        float ___coloredLayerDepth,
        Vector2 ___smallestTileSizeOrigin,
        float ___layerDepth,
        SpriteBatch b,
        Vector2 tileLocation,
        Color toTint,
        float rotation
    )
    {
        if (
            Game1.currentLocation.terrainFeatures.TryGetValue(tileLocation, out var hoeDirt)
            && hoeDirt is HoeDirt
            && hoeDirt.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME)
        )
        {
            var textureModel = AlternativeTextures.textureManager.GetSpecificTextureModel(
                hoeDirt.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME]
            );
            if (textureModel is null)
            {
                return true;
            }

            var textureVariation = Int32.Parse(hoeDirt.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION]);
            if (
                __instance.dead.Value
                || textureVariation == -1
                || AlternativeTextures.modConfig.IsTextureVariationDisabled(textureModel.GetId(), textureVariation)
            )
            {
                return true;
            }
            var textureOffset = textureModel.GetTextureOffset(textureVariation);

            var position = Game1.GlobalToLocal(Game1.viewport, ___drawPosition);

            // Handle drawing forages
            if (__instance.forageCrop.Value)
            {
                if (__instance.whichForageCrop.Value == "2")
                {
                    b.Draw(
                        textureModel.GetTexture(textureVariation),
                        Game1.GlobalToLocal(
                            Game1.viewport,
                            new Vector2(
                                (tileLocation.X * 64f)
                                    + ((((tileLocation.X * 11f) + (tileLocation.Y * 7f)) % 10f) - 5f)
                                    + 32f,
                                (tileLocation.Y * 64f)
                                    + ((((tileLocation.Y * 11f) + (tileLocation.X * 7f)) % 10f) - 5f)
                                    + 64f
                            )
                        ),
                        new Rectangle(0, textureOffset, 16, 16),
                        Color.White,
                        rotation,
                        new Vector2(8f, 16f),
                        4f,
                        SpriteEffects.None,
                        ((tileLocation.Y * 64f) + 32f + ((((tileLocation.Y * 11f) + (tileLocation.X * 7f)) % 10f) - 5f))
                            / 10000f
                    );
                }
                else
                {
                    b.Draw(
                        textureModel.GetTexture(textureVariation),
                        position,
                        new Rectangle(0, textureOffset, 16, 16),
                        Color.White,
                        0f,
                        ___smallestTileSizeOrigin,
                        4f,
                        SpriteEffects.None,
                        ___layerDepth
                    );
                }

                return false;
            }

            // Handle the crops / flowers
            var effect = __instance.flip.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            var layerDepth =
                (
                    (tileLocation.Y * 64f)
                    + 32f
                    + (
                        (
                            !__instance.shouldDrawDarkWhenWatered()
                            || __instance.currentPhase.Value >= __instance.phaseDays.Count - 1
                        )
                            ? 0f
                            : ((((tileLocation.Y * 11f) + (tileLocation.X * 7f)) % 10f) - 5f)
                    )
                )
                / 10000f
                / ((__instance.currentPhase.Value == 0 && __instance.shouldDrawDarkWhenWatered()) ? 2f : 1f);
            var sourceX = ___sourceRect.X >= 128 ? ___sourceRect.X - 128 : ___sourceRect.X;

            b.Draw(
                textureModel.GetTexture(textureVariation),
                position,
                new Rectangle(sourceX, textureOffset, 16, 32),
                toTint,
                rotation,
                ___origin,
                4f,
                effect,
                layerDepth
            );

            // Handle the tinted colors for flowers
            var tintColor = __instance.tintColor.Value;
            if (
                (!tintColor.Equals(Color.White) || textureModel.HasTint(textureVariation))
                && __instance.currentPhase.Value == __instance.phaseDays.Count - 1
                && !__instance.dead.Value
            )
            {
                if (textureModel.HasTint(textureVariation))
                {
                    tintColor = textureModel.GetRandomTint(textureVariation);
                }

                b.Draw(
                    textureModel.GetTexture(textureVariation),
                    position,
                    new Rectangle(
                        ___coloredSourceRect.X >= 128 ? ___coloredSourceRect.X - 128 : ___coloredSourceRect.X,
                        textureOffset,
                        ___coloredSourceRect.Width,
                        ___coloredSourceRect.Height
                    ),
                    tintColor,
                    rotation,
                    ___origin,
                    4f,
                    effect,
                    layerDepth + 0.01f
                );
            }

            return false;
        }
        return true;
    }

    [HarmonyBefore(["spacechase0.DynamicGameAssets"])]
    private static bool DrawWithOffsetPrefix(
        Crop __instance,
        Vector2 ___origin,
        Vector2 ___drawPosition,
        Rectangle ___sourceRect,
        Rectangle ___coloredSourceRect,
        SpriteBatch b,
        Vector2 tileLocation,
        Color toTint,
        float rotation,
        Vector2 offset
    )
    {
        if (Game1.currentLocation.getObjectAtTile((int)tileLocation.X, (int)tileLocation.Y) is not IndoorPot gardenPot)
        {
            return true;
        }

        var hoeDirt = gardenPot.hoeDirt.Value;
        if (hoeDirt != null && hoeDirt.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME))
        {
            var textureModel = AlternativeTextures.textureManager.GetSpecificTextureModel(
                hoeDirt.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME]
            );
            if (textureModel is null)
            {
                return true;
            }

            var textureVariation = Int32.Parse(hoeDirt.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION]);
            if (
                __instance.dead.Value
                || textureVariation == -1
                || AlternativeTextures.modConfig.IsTextureVariationDisabled(textureModel.GetId(), textureVariation)
            )
            {
                return true;
            }
            var textureOffset = textureModel.GetTextureOffset(textureVariation);

            var sourceX = ___sourceRect.X >= 128 ? ___sourceRect.X - 128 : ___sourceRect.X;
            if (__instance.forageCrop.Value)
            {
                b.Draw(
                    textureModel.GetTexture(textureVariation),
                    Game1.GlobalToLocal(
                        Game1.viewport,
                        offset + new Vector2(tileLocation.X * 64f, tileLocation.Y * 64f)
                    ),
                    new Rectangle(sourceX, textureOffset, 16, 32),
                    Color.White,
                    0f,
                    new Vector2(8f, 8f),
                    4f,
                    SpriteEffects.None,
                    ((tileLocation.Y + 0.66f) * 64f / 10000f) + (tileLocation.X * 1E-05f)
                );
                return false;
            }

            var tintColor = __instance.tintColor.Value;
            b.Draw(
                textureModel.GetTexture(textureVariation),
                Game1.GlobalToLocal(Game1.viewport, offset + new Vector2(tileLocation.X * 64f, tileLocation.Y * 64f)),
                new Rectangle(sourceX, textureOffset, 16, 32),
                toTint,
                rotation,
                new Vector2(8f, 24f),
                4f,
                __instance.flip.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                ((tileLocation.Y + 0.66f) * 64f / 10000f) + (tileLocation.X * 1E-05f)
            );
            if (
                (!tintColor.Equals(Color.White) || textureModel.HasTint(textureVariation))
                && __instance.currentPhase.Value == __instance.phaseDays.Count - 1
                && !__instance.dead.Value
            )
            {
                if (textureModel.HasTint(textureVariation))
                {
                    tintColor = textureModel.GetRandomTint(textureVariation);
                }

                b.Draw(
                    textureModel.GetTexture(textureVariation),
                    Game1.GlobalToLocal(
                        Game1.viewport,
                        offset + new Vector2(tileLocation.X * 64f, tileLocation.Y * 64f)
                    ),
                    new Rectangle(
                        ___coloredSourceRect.X >= 128 ? ___coloredSourceRect.X - 128 : ___coloredSourceRect.X,
                        textureOffset,
                        ___coloredSourceRect.Width,
                        ___coloredSourceRect.Height
                    ),
                    tintColor,
                    rotation,
                    new Vector2(8f, 24f),
                    4f,
                    __instance.flip.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    ((tileLocation.Y + 0.67f) * 64f / 10000f) + (tileLocation.X * 1E-05f)
                );
            }

            return false;
        }
        return true;
    }
}
