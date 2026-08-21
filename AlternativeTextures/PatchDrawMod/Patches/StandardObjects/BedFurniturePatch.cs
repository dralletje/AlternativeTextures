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

internal class BedFurniturePatch(IModHelper modHelper) : PatchTemplate()
{
    private readonly Type _object = typeof(BedFurniture);

    internal void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(
                _object,
                nameof(BedFurniture.draw),
                [typeof(SpriteBatch), typeof(int), typeof(int), typeof(float)]
            ),
            prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix))
        );
    }

    private static bool DrawPrefix(
        BedFurniture __instance,
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

            if (!__instance.isTemporarilyInvisible)
            {
                if (Furniture.isDrawingLocationFurniture)
                {
                    var sourceRect = __instance.sourceRect.Value;
                    sourceRect.X -= __instance.defaultSourceRect.X;
                    sourceRect.Y = textureOffset;

                    spriteBatch.Draw(
                        textureModel.GetTexture(textureVariation),
                        Game1.GlobalToLocal(
                            Game1.viewport,
                            ___drawPosition.Value
                                + (
                                    (__instance.shakeTimer > 0)
                                        ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2))
                                        : Vector2.Zero
                                )
                        ),
                        sourceRect,
                        Color.White * alpha,
                        0f,
                        Vector2.Zero,
                        4f,
                        __instance.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                        (float)(__instance.boundingBox.Value.Top + 1) / 10000f
                    );
                    sourceRect.X += sourceRect.Width;
                    spriteBatch.Draw(
                        textureModel.GetTexture(textureVariation),
                        Game1.GlobalToLocal(
                            Game1.viewport,
                            ___drawPosition.Value
                                + (
                                    (__instance.shakeTimer > 0)
                                        ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2))
                                        : Vector2.Zero
                                )
                        ),
                        sourceRect,
                        Color.White * alpha,
                        0f,
                        Vector2.Zero,
                        4f,
                        __instance.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                        (float)(__instance.boundingBox.Value.Bottom - 1) / 10000f
                    );
                }
                else
                {
                    __instance.draw(spriteBatch, x, y, alpha);
                }
            }
            return false;
        }
        return true;
    }
}
