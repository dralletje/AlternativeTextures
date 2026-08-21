using System;
using Incubator;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using Object = StardewValley.Object;

namespace AlternativeTextures.Framework;

static class SourceRects
{
    internal static Rectangle GetSourceRectangle(Object target, int variation, int textureWidth, int textureHeight)
    {
        var textureOffset = 0;
        var sourceRect = new Rectangle(0, textureOffset, textureWidth, textureHeight);
        if (target is Fence fence)
        {
            sourceRect = SourceRects.GetFenceSourceRect(fence, variation, textureHeight);
        }
        else if (target is Furniture furniture)
        {
            sourceRect = furniture.sourceRect.Value;
            sourceRect.X -= furniture.defaultSourceRect.X;
            sourceRect.Y = textureOffset;
        }

        if (sourceRect.Width > 32)
        {
            sourceRect.Width = 32;
        }
        return sourceRect;
    }

    public static Rectangle GetFenceSourceRect(Fence fence, int variation, int textureHeight)
    {
        var sourceRectPosition = 1;
        var textureOffset = variation == -1 ? 0 : 0;
        if (fence.health.Value > 1f || fence.repairQueued.Value)
        {
            var drawSum = fence.getDrawSum();
            sourceRectPosition = Fence.fenceDrawGuide[drawSum];

            var gateOffset = fence.isGate.Value && variation != -1 ? 128 : 0;
            if (fence.isGate.Value)
            {
                Vector2 offset = new Vector2(0f, 0f);
                switch (drawSum)
                {
                    case 10:
                        return new Rectangle(
                            (fence.gatePosition.Value == 88) ? 24 : 0,
                            textureOffset + (192 - gateOffset) + 16,
                            24,
                            32
                        );
                    case 100:
                        return new Rectangle(
                            (fence.gatePosition.Value == 88) ? 24 : 0,
                            textureOffset + (240 - gateOffset) + 16,
                            24,
                            32
                        );
                    case 1000:
                        return new Rectangle(
                            (fence.gatePosition.Value == 88) ? 24 : 0,
                            textureOffset + (288 - gateOffset),
                            24,
                            32
                        );
                    case 500:
                        return new Rectangle(
                            (fence.gatePosition.Value == 88) ? 24 : 0,
                            textureOffset + (320 - gateOffset),
                            24,
                            32
                        );
                    case 110:
                        return new Rectangle(
                            (fence.gatePosition.Value == 88) ? 24 : 0,
                            textureOffset + (128 - gateOffset),
                            24,
                            32
                        );
                    case 1500:
                        return new Rectangle(
                            (fence.gatePosition.Value == 88) ? 16 : 0,
                            textureOffset + (160 - gateOffset),
                            16,
                            16
                        );
                }
                sourceRectPosition = 5;
            }
        }

        return new Rectangle(
            sourceRectPosition * Fence.fencePieceWidth % fence.fenceTexture.Value.Bounds.Width,
            textureOffset
                + (
                    sourceRectPosition
                    * Fence.fencePieceWidth
                    / fence.fenceTexture.Value.Bounds.Width
                    * Fence.fencePieceHeight
                ),
            Fence.fencePieceWidth,
            Fence.fencePieceHeight
        );
    }

    public static Rectangle GetFlooringSourceRect(Flooring flooring, int variation, int textureHeight)
    {
        byte drawSum = 0;

        if (
            Game1.currentLocation.terrainFeatures.GetValueOrNull(flooring.Tile with { X = flooring.Tile.X + 1f })
            is Flooring
        )
        {
            drawSum = (byte)(drawSum + 2);
        }
        if (
            Game1.currentLocation.terrainFeatures.GetValueOrNull(flooring.Tile with { X = flooring.Tile.X - 1f })
            is Flooring
        )
        {
            drawSum = (byte)(drawSum + 8);
        }
        if (
            Game1.currentLocation.terrainFeatures.GetValueOrNull(flooring.Tile with { Y = flooring.Tile.Y + 1f })
            is Flooring
        )
        {
            drawSum = (byte)(drawSum + 4);
        }
        if (
            Game1.currentLocation.terrainFeatures.GetValueOrNull(flooring.Tile with { Y = flooring.Tile.Y - 1f })
            is Flooring
        )
        {
            drawSum = (byte)(drawSum + 1);
        }

        var sourceRectPosition = Flooring.drawGuide[drawSum];

        if (variation == -1)
        {
            var textureCorner = flooring.GetTextureCorner();
            return new Rectangle(
                textureCorner.X + (sourceRectPosition % 16 * 16),
                textureCorner.Y + (sourceRectPosition / 16 * 16),
                16,
                16
            );
        }

        return new Rectangle(sourceRectPosition % 16 * 16, sourceRectPosition / 16 * 16, 16, 16);
    }

    public static Rectangle GetTreeSourceRect(Tree tree, int variation, int textureHeight)
    {
        var source_rect = Tree.treeTopSourceRect;

        // TODO: Review if this code block is actually used
        /*
        if (tree.treeType.Value == 9)
        {
            if (tree.hasSeed.Value)
            {
                source_rect.X = 48;
            }
            else
            {
                source_rect.X = 0;
            }
        }
        */
        return source_rect;
    }

    public static Rectangle GetFruitTreeSourceRect(FruitTree fruitTree, int variation, int textureHeight)
    {
        if (variation == -1)
        {
            return new Rectangle(
                (
                    12
                    + (
                        (
                            fruitTree.IgnoresSeasonsHere()
                                ? 1
                                : Utility.getSeasonNumber(Game1.GetSeasonForLocation(Game1.currentLocation).ToString())
                        ) * 3
                    )
                ) * 16,
                fruitTree.GetSpriteRowNumber() * 5 * 16,
                48,
                80
            );
        }

        var sourceRectOffset = variation == -1 ? 0 : 0;
        Rectangle source_rect = new Rectangle(
            (
                12
                + (
                    (
                        fruitTree.IgnoresSeasonsHere()
                            ? 1
                            : Utility.getSeasonNumber(Game1.GetSeasonForLocation(Game1.currentLocation).ToString())
                    ) * 3
                )
            ) * 16,
            0,
            48,
            80
        );

        source_rect.Y += sourceRectOffset;
        return source_rect;
    }

    public static Rectangle GetCropSourceRect(Crop crop, int variation, int textureHeight)
    {
        if (variation == -1)
        {
            var vanillaRectangle = AlternativeTextures
                .modHelper.Reflection.GetField<Rectangle>(crop, "sourceRect")
                .GetValue();
            return new Rectangle(vanillaRectangle.X >= 128 ? 128 : 0, vanillaRectangle.Y, 128, 32);
        }

        Rectangle source_rect = new Rectangle(0, 0, 128, 32);
        return source_rect;
    }

    public static Rectangle GetGrassSourceRect(Grass grass, int variation, int textureHeight)
    {
        if (variation == -1)
        {
            return new Rectangle(0, grass.grassSourceOffset.Value, 15, 20);
        }

        Rectangle source_rect = new Rectangle(0, 0, 15, 20);
        return source_rect;
    }

    public static Rectangle GetBushSourceRect(Bush bush, int variation, int textureHeight)
    {
        if (variation == -1)
        {
            bush.setUpSourceRect();
            return AlternativeTextures.modHelper.Reflection.GetField<NetRectangle>(bush, "sourceRect").GetValue().Value;
        }

        if (bush.size.Value == Bush.greenTeaBush)
        {
            return new Rectangle(
                (Math.Min(2, bush.getAge() / 10) * 16) + (bush.tileSheetOffset.Value * 16),
                variation,
                16,
                32
            );
        }
        var vanillaSourceRect = AlternativeTextures
            .modHelper.Reflection.GetField<NetRectangle>(bush, "sourceRect")
            .GetValue();
        return new Rectangle(
            bush.tileSheetOffset.Value == 1 && bush.inBloom() ? 32 : 0,
            0,
            vanillaSourceRect.Width,
            vanillaSourceRect.Height
        );
    }

    public static Rectangle GetCharacterSourceRectangle(
        Character character,
        int variation,
        int textureWidth,
        int textureHeight
    )
    {
        var sourceRectOffset = 0;
        var sourceRect = character.Sprite.sourceRect;

        sourceRect.Y =
            sourceRectOffset
            + (
                character.Sprite.currentFrame
                * character.Sprite.SpriteWidth
                / character.Sprite.Texture.Width
                * character.Sprite.SpriteHeight
            );
        return sourceRect;
    }
}
