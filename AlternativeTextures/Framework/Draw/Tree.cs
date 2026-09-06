// using System;
// using System.Collections.Generic;
// using System.Linq;
// using AlternativeTextures.App.UI;
// using Incubator;
// using Incubator.MonoGame;
// using Microsoft.Xna.Framework;
// using Microsoft.Xna.Framework.Graphics;
// using StardewValley;
// using StardewValley.GameData.FloorsAndPaths;
// using StardewValley.GameData.WildTrees;
// using StardewValley.Objects;
// using StardewValley.TerrainFeatures;

// namespace AlternativeTextures.Framework.Paintable;

// record PlainTree
// {
//     public required string Type;

//     public int GrowthStage = 5;
//     public float health = 5;
//     public bool flipped = false;
//     public bool stump = true;
//     public bool tapped = false;
//     public bool hasSeed = false;
//     public bool hasMoss = false;
// }

// static class DrawTree
// {
//     // public static ITexture? PreviewTextureForTree(UniqueTextureIdentifier textureIdentifier, WorldObject? maybeRelated)
//     // {
//     //     if (textureIdentifier.IsDefault)
//     //     {
//     //         return null;
//     //         // return new SubTexture(tree.texture.Value, SourceRects.GetTreeSourceRect(tree, variation, 0)),
//     //     }
//     //     else
//     //     {
//     //         if (AlternativeTextures.textureManager.GetTexture(textureIdentifier) is not { } textureModel)
//     //             return null;

//     //         return new SubTexture(
//     //             textureModel.Texture,
//     //             SourceRects.GetTreeSourceRect(tree, textureModel.Variation, textureModel.TextureHeight)
//     //         );
//     //     }
//     // }

//     static WildTreeData? GetTreeData(string type)
//     {
//         return null!;
//     }

//     static string? GetDefaultTexture(PlainTree tree, GameLocation? location)
//     {
//         if (Tree.TryGetData(tree.Type, out var data) && data.Textures?.Count > 0)
//         {
//             foreach (var entry in data.Textures)
//             {
//                 if (location != null && location.IsGreenhouse && entry.Season.HasValue)
//                 {
//                     if (entry.Season == Season.Spring)
//                     {
//                         return entry.Texture;
//                     }
//                 }
//                 else if (
//                     (!entry.Season.HasValue || entry.Season == location?.GetSeason())
//                     && (entry.Condition == null || GameStateQuery.CheckConditions(entry.Condition, location))
//                 )
//                 {
//                     return entry.Texture;
//                 }
//             }
//             return data.Textures[0].Texture;
//         }
//         return null;
//     }

//     public override ITexture? drawInMenu(PlainTree tree, GameLocation location)
//     {
//         var textureName = GetDefaultTexture(tree, location);
//         var texture = Game1.content.Load<Texture2D>(textureName);

//         // layerDepth += positionOnScreen.X / 100000f;
//         if (tree.GrowthStage < 5)
//         {
//             var sourceRect = tree.GrowthStage switch
//             {
//                 0 => new Rectangle(32, 128, 16, 16),
//                 1 => new Rectangle(0, 128, 16, 16),
//                 2 => new Rectangle(16, 128, 16, 16),
//                 _ => new Rectangle(0, 96, 16, 32),
//             };

//             return spriteBatch.Draw(
//                 new IdentityTexture(texture),
//                 new Vector2(0f, (float)value.Height),
//                 value,
//                 Color.White,
//                 0f,
//                 Vector2.Zero,
//                 flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
//                 layerDepth + (positionOnScreen.Y + (float)value.Height * scale) / 20000f
//             );
//             return;
//         }

//         if (!falling.Value)
//         {
//             spriteBatch.Draw(
//                 texture.Value,
//                 positionOnScreen + new Vector2(0f, -64f * scale),
//                 new Microsoft.Xna.Framework.Rectangle(32, 96, 16, 32),
//                 Color.White,
//                 0f,
//                 Vector2.Zero,
//                 scale,
//                 flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
//                 layerDepth + (positionOnScreen.Y + 448f * scale - 1f) / 20000f
//             );
//         }

//         if (!stump.Value || falling.Value)
//         {
//             spriteBatch.Draw(
//                 texture.Value,
//                 positionOnScreen + new Vector2(-64f * scale, -320f * scale),
//                 new Microsoft.Xna.Framework.Rectangle(0, 0, 48, 96),
//                 Color.White,
//                 shakeRotation,
//                 Vector2.Zero,
//                 scale,
//                 flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
//                 layerDepth + (positionOnScreen.Y + 448f * scale) / 20000f
//             );
//         }
//     }

//     // public static ITexture? PreviewTextureForTree(PlainTree tree)
//     // {
//     //     Vector2 tileLocation = Tile;

//     //     if (tree.growthStage < 5)
//     //     {
//     //         var sourceRect = tree.growthStage switch
//     //         {
//     //             0 => new Rectangle(32, 128, 16, 16),
//     //             1 => new Rectangle(0, 128, 16, 16),
//     //             2 => new Rectangle(16, 128, 16, 16),
//     //             _ => new Rectangle(0, 96, 16, 32),
//     //         };

//     //         spriteBatch.Draw(
//     //             texture.Value,
//     //             Game1.GlobalToLocal(
//     //                 Game1.viewport,
//     //                 new Vector2(
//     //                     tileLocation.X * 64f + 32f,
//     //                     tileLocation.Y * 64f
//     //                         - (float)(sourceRect.Height * 4 - 64)
//     //                         + (float)((growthStage.Value >= 3) ? 128 : 64)
//     //                 )
//     //             ),
//     //             sourceRect,
//     //             fertilized.Value ? Color.HotPink : Color.White,
//     //             shakeRotation,
//     //             new Vector2(8f, (growthStage.Value >= 3) ? 32 : 16),
//     //             4f,
//     //             flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
//     //             (growthStage.Value == 0) ? 0.0001f : (baseSortPosition / 10000f)
//     //         );
//     //     }
//     //     else
//     //     {
//     //         if (!stump.Value || falling.Value)
//     //         {
//     //             if (IsLeafy())
//     //             {
//     //                 spriteBatch.Draw(
//     //                     Game1.mouseCursors,
//     //                     Game1.GlobalToLocal(
//     //                         Game1.viewport,
//     //                         new Vector2(tileLocation.X * 64f - 51f, tileLocation.Y * 64f - 16f)
//     //                     ),
//     //                     shadowSourceRect,
//     //                     Color.White * ((float)Math.PI / 2f - Math.Abs(shakeRotation)),
//     //                     0f,
//     //                     Vector2.Zero,
//     //                     4f,
//     //                     flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
//     //                     1E-06f
//     //                 );
//     //             }
//     //             else
//     //             {
//     //                 spriteBatch.Draw(
//     //                     Game1.mouseCursors_1_6,
//     //                     Game1.GlobalToLocal(
//     //                         Game1.viewport,
//     //                         new Vector2(tileLocation.X * 64f - 51f, tileLocation.Y * 64f - 16f)
//     //                     ),
//     //                     new Microsoft.Xna.Framework.Rectangle(469, 298, 42, 31),
//     //                     Color.White * ((float)Math.PI / 2f - Math.Abs(shakeRotation)),
//     //                     0f,
//     //                     Vector2.Zero,
//     //                     4f,
//     //                     flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
//     //                     1E-06f
//     //                 );
//     //             }
//     //             Microsoft.Xna.Framework.Rectangle source_rect = treeTopSourceRect;
//     //             if (
//     //                 (data.UseAlternateSpriteWhenSeedReady && hasSeed.Value)
//     //                 || (data.UseAlternateSpriteWhenNotShaken && !wasShakenToday.Value)
//     //             )
//     //             {
//     //                 source_rect.X = 48;
//     //             }
//     //             else
//     //             {
//     //                 source_rect.X = 0;
//     //             }
//     //             if (hasMoss.Value)
//     //             {
//     //                 source_rect.X = 96;
//     //             }
//     //             spriteBatch.Draw(
//     //                 texture.Value,
//     //                 Game1.GlobalToLocal(
//     //                     Game1.viewport,
//     //                     new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f + 64f)
//     //                 ),
//     //                 source_rect,
//     //                 Color.White * alpha,
//     //                 shakeRotation,
//     //                 new Vector2(24f, 96f),
//     //                 4f,
//     //                 flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
//     //                 (baseSortPosition + 2f) / 10000f - tileLocation.X / 1000000f
//     //             );
//     //         }
//     //         Microsoft.Xna.Framework.Rectangle stumpSource = stumpSourceRect;
//     //         if (hasMoss.Value)
//     //         {
//     //             stumpSource.X += 96;
//     //         }
//     //         if (health.Value >= 1f || (!falling.Value && health.Value > -99f))
//     //         {
//     //             spriteBatch.Draw(
//     //                 texture.Value,
//     //                 Game1.GlobalToLocal(
//     //                     Game1.viewport,
//     //                     new Vector2(
//     //                         tileLocation.X * 64f
//     //                             + ((shakeTimer > 0f) ? ((float)Math.Sin(Math.PI * 2.0 / (double)shakeTimer) * 3f) : 0f),
//     //                         tileLocation.Y * 64f - 64f
//     //                     )
//     //                 ),
//     //                 stumpSource,
//     //                 Color.White * alpha,
//     //                 0f,
//     //                 Vector2.Zero,
//     //                 4f,
//     //                 flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
//     //                 baseSortPosition / 10000f
//     //             );
//     //         }
//     //         if (stump.Value && health.Value < 4f && health.Value > -99f)
//     //         {
//     //             spriteBatch.Draw(
//     //                 texture.Value,
//     //                 Game1.GlobalToLocal(
//     //                     Game1.viewport,
//     //                     new Vector2(
//     //                         tileLocation.X * 64f
//     //                             + ((shakeTimer > 0f) ? ((float)Math.Sin(Math.PI * 2.0 / (double)shakeTimer) * 3f) : 0f),
//     //                         tileLocation.Y * 64f
//     //                     )
//     //                 ),
//     //                 new Microsoft.Xna.Framework.Rectangle(Math.Min(2, (int)(3f - health.Value)) * 16, 144, 16, 16),
//     //                 Color.White * alpha,
//     //                 0f,
//     //                 Vector2.Zero,
//     //                 4f,
//     //                 flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
//     //                 (baseSortPosition + 1f) / 10000f
//     //             );
//     //         }
//     //     }
//     //     foreach (Leaf l in leaves)
//     //     {
//     //         spriteBatch.Draw(
//     //             texture.Value,
//     //             Game1.GlobalToLocal(Game1.viewport, l.position),
//     //             new Microsoft.Xna.Framework.Rectangle(16 + l.type % 2 * 8, 112 + l.type / 2 * 8, 8, 8),
//     //             Color.White,
//     //             l.rotation,
//     //             Vector2.Zero,
//     //             4f,
//     //             SpriteEffects.None,
//     //             baseSortPosition / 10000f + 0.01f
//     //         );
//     //     }
//     // }
// }
