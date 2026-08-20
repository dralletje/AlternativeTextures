using System;
using System.Collections.Generic;
using AlternativeTextures.Framework.Models;
using AlternativeTextures.Framework.Utilities;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TerrainFeatures;

namespace AlternativeTextures.Framework.Patches.StandardObjects;

internal class FruitTreePatch(IModHelper modHelper) : PatchTemplate()
{
    private readonly Type _object = typeof(FruitTree);

    internal void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(_object, nameof(FruitTree.draw), [typeof(SpriteBatch)]),
            prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix))
        );
        harmony.Patch(
            AccessTools.Method(_object, nameof(FruitTree.seasonUpdate), [typeof(bool)]),
            postfix: new HarmonyMethod(GetType(), nameof(SeasonUpdatePostfix))
        );

        if (PatchTemplate.IsDGAUsed())
        {
            try
            {
                if (
                    Type.GetType("DynamicGameAssets.Game.CustomFruitTree, DynamicGameAssets") is Type dgaCropType
                    && dgaCropType != null
                )
                {
                    harmony.Patch(
                        AccessTools.Method(dgaCropType, nameof(FruitTree.draw), [typeof(SpriteBatch), typeof(Vector2)]),
                        prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix))
                    );
                    harmony.Patch(
                        AccessTools.Method(dgaCropType, nameof(FruitTree.seasonUpdate), [typeof(bool)]),
                        postfix: new HarmonyMethod(GetType(), nameof(SeasonUpdatePostfix))
                    );
                }
            }
            catch (Exception ex)
            {
                Monitor.Log(
                    $"Failed to patch Dynamic Game Assets in {this.GetType().Name}: AT may not be able to override certain DGA object types!",
                    LogLevel.Warn
                );
                Monitor.Log($"Patch for DGA failed in {this.GetType().Name}: {ex}", LogLevel.Trace);
            }
        }
    }

    private static bool DrawPrefix(
        FruitTree __instance,
        float ___shakeRotation,
        float ___shakeTimer,
        float ___alpha,
        List<Leaf> ___leaves,
        NetBool ___falling,
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

            var season = Game1.GetSeasonForLocation(__instance.Location).ToString();
            if (__instance.GreenHouseTileTree)
            {
                spriteBatch.Draw(
                    Game1.mouseCursors,
                    Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f, tileLocation.Y * 64f)),
                    new Rectangle(669, 1957, 16, 16),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    4f,
                    SpriteEffects.None,
                    1E-08f
                );
            }

            var textureOffset = textureModel.GetTextureOffset(textureVariation);
            if (__instance.growthStage.Value < 4)
            {
                var positionOffset =
                    new Vector2(
                        (float)
                            Math.Max(
                                -8.0,
                                Math.Min(64.0, Math.Sin((double)(tileLocation.X * 200f) / (Math.PI * 2.0)) * -16.0)
                            ),
                        (float)
                            Math.Max(
                                -8.0,
                                Math.Min(64.0, Math.Sin((double)(tileLocation.X * 200f) / (Math.PI * 2.0)) * -16.0)
                            )
                    ) / 2f;
                var sourceRect = Rectangle.Empty;
                sourceRect = __instance.growthStage.Value switch
                {
                    0 => new Rectangle(0, textureOffset * 5 * 16, 48, 80),
                    1 => new Rectangle(48, textureOffset * 5 * 16, 48, 80),
                    2 => new Rectangle(96, textureOffset * 5 * 16, 48, 80),
                    _ => new Rectangle(144, textureOffset * 5 * 16, 48, 80),
                };
                spriteBatch.Draw(
                    textureModel.GetTexture(textureVariation),
                    Game1.GlobalToLocal(
                        Game1.viewport,
                        new Vector2(
                            (tileLocation.X * 64f) + 32f + positionOffset.X,
                            (tileLocation.Y * 64f) - (float)sourceRect.Height + 128f + positionOffset.Y
                        )
                    ),
                    sourceRect,
                    Color.White,
                    ___shakeRotation,
                    new Vector2(24f, 80f),
                    4f,
                    __instance.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    (__instance.getBoundingBox().Bottom / 10000f) - (tileLocation.X / 1000000f)
                );
            }
            else
            {
                var boundingBox = __instance.getBoundingBox();
                if (!__instance.stump.Value || ___falling.Value)
                {
                    var ignoreSeason = __instance.IgnoresSeasonsHere();
                    if (!___falling.Value)
                    {
                        spriteBatch.Draw(
                            textureModel.GetTexture(textureVariation),
                            Game1.GlobalToLocal(
                                Game1.viewport,
                                new Vector2((tileLocation.X * 64f) + 32f, (tileLocation.Y * 64f) + 64f)
                            ),
                            new Rectangle(
                                (12 + ((ignoreSeason ? 1 : Utility.getSeasonNumber(season)) * 3)) * 16,
                                (textureOffset * 5 * 16) + 64,
                                48,
                                16
                            ),
                            (__instance.struckByLightningCountdown.Value > 0)
                                ? (Color.Gray * ___alpha)
                                : (Color.White * ___alpha),
                            0f,
                            new Vector2(24f, 16f),
                            4f,
                            __instance.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                            1E-07f
                        );
                    }
                    spriteBatch.Draw(
                        textureModel.GetTexture(textureVariation),
                        Game1.GlobalToLocal(
                            Game1.viewport,
                            new Vector2((tileLocation.X * 64f) + 32f, (tileLocation.Y * 64f) + 64f)
                        ),
                        new Rectangle(
                            (12 + ((ignoreSeason ? 1 : Utility.getSeasonNumber(season)) * 3)) * 16,
                            textureOffset * 5 * 16,
                            48,
                            64
                        ),
                        (__instance.struckByLightningCountdown.Value > 0)
                            ? (Color.Gray * ___alpha)
                            : (Color.White * ___alpha),
                        ___shakeRotation,
                        new Vector2(24f, 80f),
                        4f,
                        __instance.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                        (boundingBox.Bottom / 10000f) + 0.001f - (tileLocation.X / 1000000f)
                    );
                }
                if (__instance.health.Value >= 1f || (!___falling.Value && __instance.health.Value > -99f))
                {
                    spriteBatch.Draw(
                        textureModel.GetTexture(textureVariation),
                        Game1.GlobalToLocal(
                            Game1.viewport,
                            new Vector2(
                                (tileLocation.X * 64f)
                                    + 32f
                                    + (
                                        (___shakeTimer > 0f)
                                            ? ((float)Math.Sin(Math.PI * 2.0 / (double)___shakeTimer) * 2f)
                                            : 0f
                                    ),
                                (tileLocation.Y * 64f) + 64f
                            )
                        ),
                        new Rectangle(384, (textureOffset * 5 * 16) + 48, 48, 32),
                        (__instance.struckByLightningCountdown.Value > 0)
                            ? (Color.Gray * ___alpha)
                            : (Color.White * ___alpha),
                        0f,
                        new Vector2(24f, 32f),
                        4f,
                        __instance.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                        (__instance.stump.Value && !___falling.Value)
                            ? (boundingBox.Bottom / 10000f)
                            : ((boundingBox.Bottom / 10000f) - 0.001f - (tileLocation.X / 1000000f))
                    );
                }
                for (var i = 0; i < __instance.fruit.Count; i++)
                {
                    var obj =
                        (__instance.struckByLightningCountdown.Value > 0)
                            ? ItemRegistry.GetDataOrErrorItem("(O)382")
                            : ItemRegistry.GetDataOrErrorItem(__instance.fruit[i].QualifiedItemId);
                    var texture = obj.GetTexture();
                    var sourceRect = obj.GetSourceRect();
                    switch (i)
                    {
                        case 0:
                            spriteBatch.Draw(
                                texture,
                                Game1.GlobalToLocal(
                                    Game1.viewport,
                                    new Vector2(
                                        (tileLocation.X * 64f) - 64f + (tileLocation.X * 200f % 64f / 2f),
                                        (tileLocation.Y * 64f) - 192f - (tileLocation.X % 64f / 3f)
                                    )
                                ),
                                sourceRect,
                                Color.White,
                                0f,
                                Vector2.Zero,
                                4f,
                                SpriteEffects.None,
                                ((float)boundingBox.Bottom / 10000f) + 0.002f - (tileLocation.X / 1000000f)
                            );
                            break;
                        case 1:
                            spriteBatch.Draw(
                                texture,
                                Game1.GlobalToLocal(
                                    Game1.viewport,
                                    new Vector2(
                                        (tileLocation.X * 64f) + 32f,
                                        (tileLocation.Y * 64f) - 256f + (tileLocation.X * 232f % 64f / 3f)
                                    )
                                ),
                                sourceRect,
                                Color.White,
                                0f,
                                Vector2.Zero,
                                4f,
                                SpriteEffects.None,
                                ((float)boundingBox.Bottom / 10000f) + 0.002f - (tileLocation.X / 1000000f)
                            );
                            break;
                        case 2:
                            spriteBatch.Draw(
                                texture,
                                Game1.GlobalToLocal(
                                    Game1.viewport,
                                    new Vector2(
                                        (tileLocation.X * 64f) + (tileLocation.X * 200f % 64f / 3f),
                                        (tileLocation.Y * 64f) - 160f + (tileLocation.X * 200f % 64f / 3f)
                                    )
                                ),
                                sourceRect,
                                Color.White,
                                0f,
                                Vector2.Zero,
                                4f,
                                SpriteEffects.FlipHorizontally,
                                ((float)boundingBox.Bottom / 10000f) + 0.002f - (tileLocation.X / 1000000f)
                            );
                            break;
                    }
                }
            }
            foreach (var j in ___leaves)
            {
                spriteBatch.Draw(
                    textureModel.GetTexture(textureVariation),
                    Game1.GlobalToLocal(Game1.viewport, j.position),
                    new Rectangle((24 + Utility.getSeasonNumber(season)) * 16, textureOffset * 5 * 16, 8, 8),
                    Color.White,
                    j.rotation,
                    Vector2.Zero,
                    4f,
                    SpriteEffects.None,
                    (__instance.getBoundingBox().Bottom / 10000f) + 0.01f
                );
            }

            return false;
        }
        return true;
    }

    private static void SeasonUpdatePostfix(FruitTree __instance, bool onLoad)
    {
        if (
            __instance.modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) is { } textureName
            && __instance.modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION) is { } variation
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
