using System;
using AlternativeTextures.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace AlternativeTextures.PatchDrawMod.Patches.StandardObjects;

internal class FishTankFurniturePatch(IModHelper modHelper) : PatchTemplate()
{
    private readonly Type _object = typeof(FishTankFurniture);

    internal void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(
                _object,
                nameof(FishTankFurniture.draw),
                [typeof(SpriteBatch), typeof(int), typeof(int), typeof(float)]
            ),
            prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix))
        );

        if (PatchTemplate.IsDGAUsed())
        {
            try
            {
                if (
                    Type.GetType("DynamicGameAssets.Game.CustomFishTankFurniture, DynamicGameAssets")
                        is Type dgaFishTankFurnitureType
                    && dgaFishTankFurnitureType != null
                )
                {
                    harmony.Patch(
                        AccessTools.Method(
                            dgaFishTankFurnitureType,
                            nameof(FishTankFurniture.draw),
                            [typeof(SpriteBatch), typeof(int), typeof(int), typeof(float)]
                        ),
                        prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix))
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
        FishTankFurniture __instance,
        NetInt ___sourceIndexOffset,
        NetVector2 ___drawPosition,
        SpriteBatch spriteBatch,
        int x,
        int y,
        float alpha = 1f
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
            var textureOffset = textureModel.GetTextureOffset(textureVariation);

            var shake = Vector2.Zero;
            if (!__instance.isTemporarilyInvisible)
            {
                var draw_position = ___drawPosition.Value;
                if (!Furniture.isDrawingLocationFurniture)
                {
                    draw_position = new Vector2(x, y) * 64f;
                    draw_position.Y -= (__instance.sourceRect.Height * 4) - __instance.boundingBox.Height;
                }
                if (__instance.shakeTimer > 0)
                {
                    shake = new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
                }

                Rectangle sourceRect = new Rectangle(
                    __instance.sourceRect.Value.Width,
                    __instance.sourceRect.Value.Y,
                    __instance.sourceRect.Value.Width,
                    __instance.sourceRect.Value.Height
                )
                {
                    Y = textureOffset,
                };

                spriteBatch.Draw(
                    textureModel.GetTexture(textureVariation),
                    Game1.GlobalToLocal(Game1.viewport, draw_position + shake),
                    sourceRect,
                    Color.White * alpha,
                    0f,
                    Vector2.Zero,
                    4f,
                    __instance.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    __instance.GetGlassDrawLayer()
                );
                if (Furniture.isDrawingLocationFurniture)
                {
                    var hatsDrawn = 0;
                    for (var i = 0; i < __instance.tankFish.Count; i++)
                    {
                        var fish = __instance.tankFish[i];
                        var fish_layer = Utility.Lerp(
                            __instance.GetFishSortRegion().Y,
                            __instance.GetFishSortRegion().X,
                            fish.zPosition / 20f
                        );
                        fish_layer += 1E-07f * (float)i;
                        fish.Draw(spriteBatch, alpha, fish_layer);
                        if (fish.fishIndex != 86)
                        {
                            continue;
                        }
                        var hatsSoFar = 0;
                        foreach (var h in __instance.heldItems)
                        {
                            if (h is Hat)
                            {
                                if (hatsSoFar == hatsDrawn)
                                {
                                    h.drawInMenu(
                                        spriteBatch,
                                        Game1.GlobalToLocal(
                                            fish.GetWorldPosition()
                                                + new Vector2(-30 + (fish.facingLeft ? (-4) : 0), -55f)
                                        ),
                                        0.75f,
                                        1f,
                                        fish_layer + 1E-08f,
                                        StackDrawType.Hide
                                    );
                                    hatsDrawn++;
                                    break;
                                }
                                hatsSoFar++;
                            }
                        }
                    }
                    for (var j = 0; j < __instance.floorDecorations.Count; j++)
                    {
                        if (__instance.floorDecorations[j] is { } decoration)
                        {
                            var decoration_position = decoration.Value;
                            var decoration_source_rect = decoration.Key;
                            var decoration_layer =
                                Utility.Lerp(
                                    __instance.GetFishSortRegion().Y,
                                    __instance.GetFishSortRegion().X,
                                    decoration_position.Y / 20f
                                ) - 1E-06f;
                            spriteBatch.Draw(
                                __instance.GetAquariumTexture(),
                                Game1.GlobalToLocal(
                                    new Vector2(
                                        (float)__instance.GetTankBounds().Left + (decoration_position.X * 4f),
                                        (float)(__instance.GetTankBounds().Bottom - 4) - (decoration_position.Y * 4f)
                                    )
                                ),
                                decoration_source_rect,
                                Color.White * alpha,
                                0f,
                                new Vector2(decoration_source_rect.Width / 2, decoration_source_rect.Height - 4),
                                4f,
                                SpriteEffects.None,
                                decoration_layer
                            );
                        }
                    }
                    foreach (var bubble in __instance.bubbles)
                    {
                        var layer =
                            Utility.Lerp(
                                __instance.GetFishSortRegion().Y,
                                __instance.GetFishSortRegion().X,
                                bubble.Z / 20f
                            ) - 1E-06f;
                        spriteBatch.Draw(
                            __instance.GetAquariumTexture(),
                            Game1.GlobalToLocal(
                                new Vector2(
                                    (float)__instance.GetTankBounds().Left + bubble.X,
                                    (float)(__instance.GetTankBounds().Bottom - 4) - bubble.Y - (bubble.Z * 4f)
                                )
                            ),
                            new Rectangle(0, 240, 16, 16),
                            Color.White * alpha,
                            0f,
                            new Vector2(8f, 8f),
                            4f * bubble.W,
                            SpriteEffects.None,
                            layer
                        );
                    }
                }
                FurniturePatch.DrawPrefix(__instance, ___sourceIndexOffset, ___drawPosition, spriteBatch, x, y, alpha);
            }

            return false;
        }
        return true;
    }
}
