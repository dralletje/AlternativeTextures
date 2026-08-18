using System;
using System.Collections.Generic;
using System.Linq;
using AlternativeTextures.Framework.Models;
using AlternativeTextures.Framework.Patches;
using AlternativeTextures.Framework.Patches.Buildings;
using AlternativeTextures.Framework.Utilities;
using ConsoleLog;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using Newtonsoft.Json;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.GameData.FloorsAndPaths;
using StardewValley.GameData.GiantCrops;
using StardewValley.Internal;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using static AlternativeTextures.Framework.Models.AlternativeTextureModel;
using Object = StardewValley.Object;

namespace AlternativeTextures.Framework;

static class SourceRects
{
    internal static Rectangle GetSourceRectangle(
        AlternativeTextureModel textureModel,
        Object target,
        int textureWidth,
        int textureHeight,
        int variation
    )
    {
        var textureOffset = variation > 0 ? textureModel.GetTextureOffset(variation) : 0;
        var sourceRect = new Rectangle(0, textureOffset, textureWidth, textureHeight);
        if (target is Fence fence)
        {
            sourceRect = SourceRects.GetFenceSourceRect(
                textureModel,
                fence,
                textureHeight,
                variation
            );
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

    public static Rectangle GetFenceSourceRect(
        AlternativeTextureModel textureModel,
        Fence fence,
        int textureHeight,
        int variation
    )
    {
        int sourceRectPosition = 1;
        var textureOffset = variation == -1 ? 0 : textureModel.GetTextureOffset(variation);
        if (fence.health.Value > 1f || fence.repairQueued.Value)
        {
            int drawSum = fence.getDrawSum();
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
            (sourceRectPosition * Fence.fencePieceWidth % fence.fenceTexture.Value.Bounds.Width),
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

    public static Rectangle GetFlooringSourceRect(
        AlternativeTextureModel textureModel,
        Flooring flooring,
        int textureHeight,
        int variation
    )
    {
        byte drawSum = 0;
        Vector2 surroundingLocations = flooring.Tile;
        surroundingLocations.X += 1f;
        if (
            Game1.currentLocation.terrainFeatures.ContainsKey(surroundingLocations)
            && Game1.currentLocation.terrainFeatures[surroundingLocations] is Flooring
        )
        {
            drawSum = (byte)(drawSum + 2);
        }
        surroundingLocations.X -= 2f;
        if (
            Game1.currentLocation.terrainFeatures.ContainsKey(surroundingLocations)
            && Game1.currentLocation.terrainFeatures[surroundingLocations] is Flooring
        )
        {
            drawSum = (byte)(drawSum + 8);
        }
        surroundingLocations.X += 1f;
        surroundingLocations.Y += 1f;
        if (
            Game1.currentLocation.terrainFeatures.ContainsKey(surroundingLocations)
            && Game1.currentLocation.terrainFeatures[surroundingLocations] is Flooring
        )
        {
            drawSum = (byte)(drawSum + 4);
        }
        surroundingLocations.Y -= 2f;
        if (
            Game1.currentLocation.terrainFeatures.ContainsKey(surroundingLocations)
            && Game1.currentLocation.terrainFeatures[surroundingLocations] is Flooring
        )
        {
            drawSum = (byte)(drawSum + 1);
        }

        int sourceRectPosition = Flooring.drawGuide[drawSum];

        if (variation == -1)
        {
            Point textureCorner = flooring.GetTextureCorner();
            return new Rectangle(
                textureCorner.X + sourceRectPosition % 16 * 16,
                textureCorner.Y + sourceRectPosition / 16 * 16,
                16,
                16
            );
        }

        int sourceRectOffset = textureModel?.GetTextureOffset(variation) ?? 0;
        return new Rectangle(
            sourceRectPosition % 16 * 16,
            sourceRectPosition / 16 * 16 + sourceRectOffset,
            16,
            16
        );
    }

    public static Rectangle GetTreeSourceRect(
        AlternativeTextureModel textureModel,
        Tree tree,
        int textureHeight,
        int variation
    )
    {
        int sourceRectOffset = variation == -1 ? 0 : textureModel.GetTextureOffset(variation);
        Rectangle source_rect = Tree.treeTopSourceRect;

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

        source_rect.Y += sourceRectOffset;
        return source_rect;
    }

    public static Rectangle GetFruitTreeSourceRect(
        AlternativeTextureModel textureModel,
        FruitTree fruitTree,
        int textureHeight,
        int variation
    )
    {
        if (variation == -1)
        {
            return new Rectangle(
                (
                    12
                    + (
                        fruitTree.IgnoresSeasonsHere()
                            ? 1
                            : Utility.getSeasonNumber(
                                Game1.GetSeasonForLocation(Game1.currentLocation).ToString()
                            )
                    ) * 3
                ) * 16,
                fruitTree.GetSpriteRowNumber() * 5 * 16,
                48,
                80
            );
        }

        int sourceRectOffset = variation == -1 ? 0 : textureModel.GetTextureOffset(variation);
        Rectangle source_rect = new Rectangle(
            (
                12
                + (
                    fruitTree.IgnoresSeasonsHere()
                        ? 1
                        : Utility.getSeasonNumber(
                            Game1.GetSeasonForLocation(Game1.currentLocation).ToString()
                        )
                ) * 3
            ) * 16,
            0,
            48,
            80
        );

        source_rect.Y += sourceRectOffset;
        return source_rect;
    }

    public static Rectangle GetCropSourceRect(
        AlternativeTextureModel textureModel,
        Crop crop,
        int textureHeight,
        int variation
    )
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

    public static Rectangle GetGrassSourceRect(
        AlternativeTextureModel textureModel,
        Grass grass,
        int textureHeight,
        int variation
    )
    {
        if (variation == -1)
        {
            return new Rectangle(0, grass.grassSourceOffset.Value, 15, 20);
        }

        Rectangle source_rect = new Rectangle(0, 0, 15, 20);
        return source_rect;
    }

    public static Rectangle GetBushSourceRect(
        AlternativeTextureModel textureModel,
        Bush bush,
        int textureHeight,
        int variation
    )
    {
        if (variation == -1)
        {
            bush.setUpSourceRect();
            return AlternativeTextures
                .modHelper.Reflection.GetField<NetRectangle>(bush, "sourceRect")
                .GetValue()
                .Value;
        }

        if (bush.size.Value == Bush.greenTeaBush)
        {
            return new Rectangle(
                Math.Min(2, bush.getAge() / 10) * 16 + bush.tileSheetOffset.Value * 16,
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
        AlternativeTextureModel textureModel,
        Character character,
        int textureWidth,
        int textureHeight,
        int variation
    )
    {
        int sourceRectOffset = textureModel.GetTextureOffset(variation);
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
