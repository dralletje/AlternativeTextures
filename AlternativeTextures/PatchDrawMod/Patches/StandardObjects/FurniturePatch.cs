using System;
using AlternativeTextures.Framework;
using HarmonyLib;
using Incubator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace AlternativeTextures.PatchDrawMod.Patches.StandardObjects;

class Container<T>(T @default)
{
    public T Value = @default;
}

internal class FurniturePatch(IModHelper modHelper) : PatchTemplate()
{
    private readonly Type _object = typeof(Furniture);

    internal void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(
                _object,
                nameof(Furniture.draw),
                [typeof(SpriteBatch), typeof(int), typeof(int), typeof(float)]
            ),
            prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix))
        );

        harmony.Patch(
            AccessTools.Method(
                _object,
                nameof(Furniture.drawAtNonTileSpot),
                [typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float)]
            ),
            prefix: new HarmonyMethod(GetType(), nameof(DrawAtNonTileSpotPrefix))
        );
        // harmony.Patch(
        //     AccessTools.Method(
        //         _object,
        //         nameof(Furniture.drawInMenu),
        //         [
        //             typeof(SpriteBatch),
        //             typeof(Vector2),
        //             typeof(float),
        //             typeof(float),
        //             typeof(float),
        //             typeof(StackDrawType),
        //             typeof(Color),
        //             typeof(bool),
        //         ]
        //     ),
        //     prefix: new HarmonyMethod(GetType(), nameof(DrawInMenuPrefix))
        // );
    }

    internal static bool DrawPrefix(
        Furniture __instance,
        NetInt ___sourceIndexOffset,
        NetVector2 ___drawPosition,
        SpriteBatch spriteBatch,
        int x,
        int y,
        float alpha = 1f
    )
    {
        if (
            GetTextureForUse(
                __instance.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_NAME),
                __instance.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION)
            )
            is not { } textureModel
        )
            return true;

        // Replicate the base draw
        if (__instance.isTemporarilyInvisible)
        {
            return true;
        }

        // Set xTileOffset if AlternativeTextureModel has an animation
        var xTileOffset = 0;
        var actualSourceIndexOffset = ___sourceIndexOffset.Value;
        if (textureModel.HasAnimation())
        {
            if (
                !__instance.modData.ContainsKey("AlternativeTextureCurrentFrame")
                || !__instance.modData.ContainsKey("AlternativeTextureFrameIndex")
                || !__instance.modData.ContainsKey("AlternativeTextureFrameDuration")
                || !__instance.modData.ContainsKey("AlternativeTextureElapsedDuration")
            )
            {
                __instance.modData["AlternativeTextureCurrentFrame"] = "0";
                __instance.modData["AlternativeTextureFrameIndex"] = "0";
                __instance.modData["AlternativeTextureFrameDuration"] = textureModel
                    .GetAnimationDataAtIndex(0)
                    .Duration.ToString(); // Animation.ElementAt(0).Duration.ToString();
                __instance.modData["AlternativeTextureElapsedDuration"] = "0";
            }

            var currentFrame = Int32.Parse(__instance.modData["AlternativeTextureCurrentFrame"]);
            var frameIndex = Int32.Parse(__instance.modData["AlternativeTextureFrameIndex"]);
            var frameDuration = Int32.Parse(__instance.modData["AlternativeTextureFrameDuration"]);
            var elapsedDuration = Int32.Parse(__instance.modData["AlternativeTextureElapsedDuration"]);

            if (elapsedDuration >= frameDuration)
            {
                frameIndex = frameIndex + 1 >= textureModel.GetAnimationData().Count ? 0 : frameIndex + 1;

                var animationData = textureModel.GetAnimationDataAtIndex(frameIndex);
                currentFrame = animationData.Frame;

                __instance.modData["AlternativeTextureCurrentFrame"] = currentFrame.ToString();
                __instance.modData["AlternativeTextureFrameIndex"] = frameIndex.ToString();
                __instance.modData["AlternativeTextureFrameDuration"] = animationData.Duration.ToString();
                __instance.modData["AlternativeTextureElapsedDuration"] = "0";
            }
            else
            {
                __instance.modData["AlternativeTextureElapsedDuration"] = (
                    elapsedDuration + Game1.currentGameTime.ElapsedGameTime.Milliseconds
                ).ToString();
            }

            xTileOffset = currentFrame;
            actualSourceIndexOffset = 0;
        }

        var sourceRect = __instance.sourceRect.Value with
        {
            X =
                __instance.sourceRect.Value.X
                - __instance.defaultSourceRect.X
                + (actualSourceIndexOffset * __instance.sourceRect.Value.Width)
                + (xTileOffset * __instance.sourceRect.Value.Width),
            Y = 0,
        };

        if (Furniture.isDrawingLocationFurniture)
        {
            Texture2D? frontTexture = null;
            if (__instance.HasSittingFarmers())
            {
                try
                {
                    frontTexture = Game1.content.Load<Texture2D>(
                        ItemRegistry.GetDataOrErrorItem(__instance.QualifiedItemId).TextureName + "Front"
                    );
                }
                catch
                {
                    frontTexture = null;
                }
            }

            if (
                frontTexture is not null
                && __instance.sourceRect.Right <= frontTexture.Width
                && __instance.sourceRect.Bottom <= frontTexture.Height
            )
            {
                spriteBatch.Draw(
                    textureModel.Texture,
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
                    (float)(__instance.boundingBox.Value.Top + 16) / 10000f
                );

                var rotationSourceRect = sourceRect;
                rotationSourceRect.Y += textureModel.TextureHeight / 2;
                spriteBatch.Draw(
                    textureModel.Texture,
                    Game1.GlobalToLocal(
                        Game1.viewport,
                        ___drawPosition.Value
                            + (
                                (__instance.shakeTimer > 0)
                                    ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2))
                                    : Vector2.Zero
                            )
                    ),
                    rotationSourceRect,
                    Color.White * alpha,
                    0f,
                    Vector2.Zero,
                    4f,
                    __instance.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    (float)(__instance.boundingBox.Value.Bottom - 8) / 10000f
                );
            }
            else
            {
                spriteBatch.Draw(
                    textureModel.Texture,
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
                    (__instance.furniture_type.Value == 12)
                        ? (2E-09f + (__instance.TileLocation.Y / 100000f))
                        : (
                            (float)(
                                __instance.boundingBox.Value.Bottom
                                - ((__instance.furniture_type.Value is 6 or 17 or 13) ? 48 : 8)
                            ) / 10000f
                        )
                );
            }
        }
        else
        {
            spriteBatch.Draw(
                textureModel.Texture,
                Game1.GlobalToLocal(
                    Game1.viewport,
                    new Vector2(
                        (x * 64) + ((__instance.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0),
                        (y * 64)
                            - ((__instance.sourceRect.Height * 4) - __instance.boundingBox.Height)
                            + ((__instance.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0)
                    )
                ),
                __instance.sourceRect.Value,
                Color.White * alpha,
                0f,
                Vector2.Zero,
                4f,
                __instance.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                (__instance.furniture_type.Value == 12)
                    ? (2E-09f + (__instance.TileLocation.Y / 100000f))
                    : (
                        (float)(
                            __instance.boundingBox.Value.Bottom
                            - ((__instance.furniture_type.Value is 6 or 17 or 13) ? 48 : 8)
                        ) / 10000f
                    )
            );
        }
        if (__instance.heldObject.Value != null)
        {
            if (__instance.heldObject.Value is Furniture furniture)
            {
                furniture.drawAtNonTileSpot(
                    spriteBatch,
                    Game1.GlobalToLocal(
                        Game1.viewport,
                        new Vector2(
                            __instance.boundingBox.Center.X - 32,
                            __instance.boundingBox.Center.Y
                                - (furniture.sourceRect.Height * 4)
                                - (__instance.drawHeldObjectLow.Value ? (-16) : 16)
                        )
                    ),
                    (float)(__instance.boundingBox.Bottom - 7) / 10000f,
                    alpha
                );
            }
            else if (
                HeldObjectDraw(
                    __instance.heldObject.Value,
                    spriteBatch,
                    Game1.GlobalToLocal(
                        Game1.viewport,
                        new Vector2(
                            __instance.boundingBox.Center.X - 32,
                            __instance.boundingBox.Center.Y - (__instance.drawHeldObjectLow.Value ? 32 : 85)
                        )
                    ),
                    (float)(__instance.boundingBox.Bottom + 1) / 10000f,
                    alpha
                )
            )
            {
                var heldItemData = ItemRegistry.GetDataOrErrorItem(__instance.heldObject.Value.QualifiedItemId);
                spriteBatch.Draw(
                    Game1.shadowTexture,
                    Game1.GlobalToLocal(
                        Game1.viewport,
                        new Vector2(
                            __instance.boundingBox.Center.X - 32,
                            __instance.boundingBox.Center.Y - (__instance.drawHeldObjectLow.Value ? 32 : 85)
                        )
                    ) + new Vector2(32f, 53f),
                    Game1.shadowTexture.Bounds,
                    Color.White * alpha,
                    0f,
                    new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y),
                    4f,
                    SpriteEffects.None,
                    (float)__instance.boundingBox.Bottom / 10000f
                );
                if (__instance.heldObject.Value is ColoredObject)
                {
                    __instance.heldObject.Value.drawInMenu(
                        spriteBatch,
                        Game1.GlobalToLocal(
                            Game1.viewport,
                            new Vector2(
                                __instance.boundingBox.Center.X - 32,
                                __instance.boundingBox.Center.Y - (__instance.drawHeldObjectLow.Value ? 32 : 85)
                            )
                        ),
                        1f,
                        1f,
                        (float)(__instance.boundingBox.Bottom + 1) / 10000f,
                        StackDrawType.Hide,
                        Color.White,
                        drawShadow: false
                    );
                }
                else
                {
                    spriteBatch.Draw(
                        heldItemData.GetTexture(),
                        Game1.GlobalToLocal(
                            Game1.viewport,
                            new Vector2(
                                __instance.boundingBox.Center.X - 32,
                                __instance.boundingBox.Center.Y - (__instance.drawHeldObjectLow.Value ? 32 : 85)
                            )
                        ),
                        heldItemData.GetSourceRect(),
                        Color.White * alpha,
                        0f,
                        Vector2.Zero,
                        4f,
                        SpriteEffects.None,
                        (float)(__instance.boundingBox.Bottom + 1) / 10000f
                    );
                }
            }
        }
        if (__instance.IsOn && __instance.furniture_type.Value == 14)
        {
            spriteBatch.Draw(
                Game1.mouseCursors,
                Game1.GlobalToLocal(
                    Game1.viewport,
                    new Vector2(__instance.boundingBox.Center.X - 12, __instance.boundingBox.Center.Y - 64)
                ),
                new Rectangle(
                    276
                        + (
                            (int)(
                                (
                                    Game1.currentGameTime.TotalGameTime.TotalMilliseconds
                                    + (double)(x * 3047)
                                    + (double)(y * 88)
                                )
                                % 400.0
                                / 100.0
                            ) * 12
                        ),
                    1985,
                    12,
                    11
                ),
                Color.White,
                0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                (float)(__instance.GetBoundingBoxAt(x, y).Bottom - 2) / 10000f
            );
            spriteBatch.Draw(
                Game1.mouseCursors,
                Game1.GlobalToLocal(
                    Game1.viewport,
                    new Vector2(__instance.boundingBox.Center.X - 32 - 4, __instance.boundingBox.Center.Y - 64)
                ),
                new Rectangle(
                    276
                        + (
                            (int)(
                                (
                                    Game1.currentGameTime.TotalGameTime.TotalMilliseconds
                                    + (double)(x * 2047)
                                    + (double)(y * 98)
                                )
                                % 400.0
                                / 100.0
                            ) * 12
                        ),
                    1985,
                    12,
                    11
                ),
                Color.White,
                0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                (float)(__instance.GetBoundingBoxAt(x, y).Bottom - 1) / 10000f
            );
        }
        else if (__instance.IsOn && __instance.furniture_type.Value == 16)
        {
            spriteBatch.Draw(
                Game1.mouseCursors,
                Game1.GlobalToLocal(
                    Game1.viewport,
                    new Vector2(__instance.boundingBox.Center.X - 20, (float)__instance.boundingBox.Center.Y - 105.6f)
                ),
                new Rectangle(
                    276
                        + (
                            (int)(
                                (
                                    Game1.currentGameTime.TotalGameTime.TotalMilliseconds
                                    + (double)(x * 3047)
                                    + (double)(y * 88)
                                )
                                % 400.0
                                / 100.0
                            ) * 12
                        ),
                    1985,
                    12,
                    11
                ),
                Color.White,
                0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                (float)(__instance.GetBoundingBoxAt(x, y).Bottom - 2) / 10000f
            );
        }

        return false;
    }

    private static bool HeldObjectDraw(
        Object __instance,
        SpriteBatch spriteBatch,
        Vector2 location,
        float layerDepth,
        float alpha = 1f
    )
    {
        if (
            GetTextureForUse(
                __instance.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_NAME),
                __instance.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION)
            )
            is not { } textureModel
        )
            return true;

        // Get the current X index for the source tile
        var xTileOffset = __instance.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_SHEET_ID)
            ? __instance.ParentSheetIndex - Int32.Parse(__instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SHEET_ID])
            : 0;
        if (__instance.showNextIndex.Value)
        {
            xTileOffset += 1;
        }
        xTileOffset *= textureModel.TextureWidth;

        // Replicate the base draw
        var sourceRect = new Rectangle(xTileOffset, 0, textureModel.TextureWidth, textureModel.TextureHeight);
        spriteBatch.Draw(
            textureModel.Texture,
            location,
            sourceRect,
            Color.White * alpha,
            0f,
            Vector2.Zero,
            4f,
            __instance.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
            layerDepth
        );

        return false;
    }

    private static bool DrawAtNonTileSpotPrefix(
        Furniture __instance,
        NetInt ___sourceIndexOffset,
        SpriteBatch spriteBatch,
        Vector2 location,
        float layerDepth,
        float alpha = 1f
    )
    {
        if (
            GetTextureForUse(
                __instance.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_NAME),
                __instance.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION)
            )
            is not { } textureModel
        )
            return true;

        // Replicate the base draw
        var sourceRect = __instance.sourceRect.Value with
        {
            X = __instance.sourceRect.Value.X - __instance.defaultSourceRect.X,
            Y = 0,
        };

        spriteBatch.Draw(
            textureModel.Texture,
            location,
            sourceRect,
            Color.White * alpha,
            0f,
            Vector2.Zero,
            4f,
            __instance.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
            layerDepth
        );

        return false;
    }

    // private static bool DrawInMenuPrefix(
    //     Furniture __instance,
    //     NetInt ___sourceIndexOffset,
    //     SpriteBatch spriteBatch,
    //     Vector2 location,
    //     float scaleSize,
    //     float transparency,
    //     float layerDepth,
    //     StackDrawType drawStackNumber,
    //     Color color,
    //     bool drawShadow
    // )
    // {
    //     if (
    //         (
    //             Game1.activeClickableMenu is PaintBucketMenu
    //         /// IMMEDIATE TODO
    //         // ||  Game1.activeClickableMenu is CatalogueMenu
    //         ) && !PatchTemplate.IsDGAObject(__instance)
    //     )
    //     {
    //         var texture = ItemRegistry.GetDataOrErrorItem(__instance.QualifiedItemId).GetTexture();
    //         var sourceRect =
    //             __instance.rotations.Value > 1 ? __instance.sourceRect.Value : __instance.defaultSourceRect.Value;

    //         if (
    //             __instance.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME)
    //             && AlternativeTextures.textureManager.GetSpecificTextureModel(
    //                 __instance.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_NAME)
    //             )
    //                 is AlternativeTextureModel textureModel
    //             && Int32.TryParse(
    //                 __instance.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION),
    //                 out var textureVariation
    //             )
    //             && textureVariation != -1
    //         )
    //         {
    //             texture = textureModel.GetTexture(textureVariation);
    //             sourceRect.X = Math.Max(0, __instance.sourceRect.X - __instance.defaultSourceRect.X);
    //             sourceRect.Y = textureModel.GetTextureOffset(textureVariation);
    //         }

    //         sourceRect.X += sourceRect.Width * ___sourceIndexOffset.Value;
    //         spriteBatch.Draw(
    //             texture,
    //             location + new Vector2(32f, 32f),
    //             sourceRect,
    //             color * transparency,
    //             0f,
    //             new Vector2(sourceRect.Width / 2, sourceRect.Height / 2),
    //             1f * GetScaleSize(sourceRect) * scaleSize,
    //             __instance.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
    //             layerDepth
    //         );

    //         return false;
    //     }
    //     return true;
    // }

    private static float GetScaleSize(Rectangle sourceRect)
    {
        var tilesWide = sourceRect.Width / 16;
        var tilesHigh = sourceRect.Height / 16;
        if (tilesWide >= 7)
        {
            return 0.5f;
        }
        if (tilesWide >= 6)
        {
            return 0.66f;
        }
        if (tilesWide >= 5)
        {
            return 0.75f;
        }
        if (tilesHigh >= 5)
        {
            return 0.8f;
        }
        if (tilesHigh >= 3)
        {
            return 1f;
        }
        if (tilesWide <= 2)
        {
            return 1.5f;
        }
        return tilesWide <= 4 ? 1f : 0.1f;
    }
}
