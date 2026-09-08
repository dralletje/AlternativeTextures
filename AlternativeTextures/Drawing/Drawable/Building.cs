// using System;
// using AlternativeTextures.Framework;
// using Dral.Sprites;
// using Microsoft.Xna.Framework;
// using Microsoft.Xna.Framework.Graphics;
// using StardewValley;
// using StardewValley.GameData.Buildings;

// namespace AlternativeTextures.Drawing;

// public closed record BuildingColor
// {
//     public record Default() : BuildingColor;
//     public record HSL(int Hue, int Saturation, int Lightness) : BuildingColor;
// }

// public closed partial record DrawableWorldObject
// {
//     public record Building(ModelIdentifier Model) : DrawableWorldObject
//     {
//         public int DaysOfConstructionLeft { get; init; } = 0;
//         public BuildingColor Color1 { get; init; } = new BuildingColor.Default();
//         public BuildingColor Color2 { get; init; } = new BuildingColor.Default();
//         public BuildingColor Color3 { get; init; } = new BuildingColor.Default();

//         BuildingData? data => Game1.buildingData.TryGetValue(Model.Name, out var data) ? data : null;

//         // public Tree With(StardewValley.TerrainFeatures.Tree? tree)
//         // {
//         //     if (tree is null)
//         //     {
//         //         return this;
//         //     }
//         //     else
//         //     {
//         //         if (ModelIdentifier.From(tree) != Model)
//         //             throw new ArgumentException(".With mismatch tree derived model and provided model");
//         //         return this with
//         //         {
//         //             GrowthStage = tree.growthStage.Value,
//         //             Health = tree.health.Value,
//         //             Flipped = tree.flipped.Value,
//         //             HasSeed = tree.hasSeed.Value,
//         //             HasMoss = tree.hasMoss.Value,
//         //             WasShakenToday = tree.wasShakenToday.Value,
//         //             Fertilized = tree.fertilized.Value,
//         //             ShakeRotation = tree.shakeRotation,
//         //         };
//         //     }
//         // }

//         static readonly TextureSprite LeftShadow = Game1.mouseCursors.Clip(StardewValley.Buildings.Building.leftShadow);
//         static readonly TextureSprite MiddleShadow = Game1.mouseCursors.Clip(StardewValley.Buildings.Building.middleShadow);
//         static readonly TextureSprite RightShadow = Game1.mouseCursors.Clip(StardewValley.Buildings.Building.rightShadow);

//         ISprite? Shadow()
//         {
//             if (data is null) return null;

//             var tilesHeight = data.Size.Y;
//             var tileWide = data.Size.X;
//             // var rectangle = getSourceRectForMenu() ?? getSourceRect();
//             // var vector = ((localX == -1) ? Game1.GlobalToLocal(new Vector2(tileX.Value * 64, (tileY.Value + tilesHigh.Value) * 64)) : new Vector2(localX, localY + rectangle.Height * 4));
//             // b.Draw(Game1.mouseCursors, vector, StardewValley.Buildings.Building.leftShadow, Color.White * ((localX == -1) ? alpha : 1f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-05f);
//             // for (int i = 1; i < tilesWide.Value - 1; i++)
//             // {
//             //     b.Draw(Game1.mouseCursors, vector + new Vector2(i * 64, 0f), StardewValley.Buildings.Building.middleShadow, Color.White * ((localX == -1) ? alpha : 1f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-05f);
//             // }

//             // b.Draw(Game1.mouseCursors, vector + new Vector2((tilesWide.Value - 1) * 64, 0f), StardewValley.Buildings.Building.rightShadow, Color.White * ((localX == -1) ? alpha : 1f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-05f);
//             return new ClippableSprite([
//                 LeftShadow,
//                 /// TODO
//             ]);
//         }

//         public Rectangle? getSourceRect()
//         {
//             /// TODO Return texture size... ???
//             /// .... Shouldn't I just error when there is no data found? :thinking:
//             if (data is null) return null;
//             {
//                 var sourceRect = data.SourceRect;
//                 if (sourceRect == Rectangle.Empty)
//                 {
//                     return null;
//                 }

//                 GameLocation gameLocation = GetIndoors();
//                 if (gameLocation is FarmHouse farmHouse)
//                 {
//                     if (gameLocation is Cabin)
//                     {
//                         sourceRect.X += sourceRect.Width * Math.Min(farmHouse.upgradeLevel, 2);
//                     }
//                     else
//                     {
//                         sourceRect.Y += sourceRect.Height * Math.Min(farmHouse.upgradeLevel, 2);
//                     }
//                 }

//                 sourceRect = ApplySourceRectOffsets(sourceRect);
//                 if (buildingType.Value == "Greenhouse" && GetParentLocation() is Farm farm && !farm.greenhouseUnlocked.Value)
//                 {
//                     sourceRect.Y -= sourceRect.Height;
//                 }

//                 return sourceRect;
//             }

//             if (isCabin)
//             {
//                 return new Microsoft.Xna.Framework.Rectangle(((GetIndoors() is Cabin cabin) ? Math.Min(cabin.upgradeLevel, 2) : 0) * 80, 0, 80, 112);
//             }

//             return texture.Value.Bounds;
//         }




//         override public ISprite? Sprite(AlternativeTextureModel? model, GameLocation? location)
//         {
//             // if (daysOfConstructionLeft.Value > 0 || newConstructionTimer.Value > 0)
//             // {
//             //     drawInConstruction(b);
//             //     return;
//             // }

//             // BuildingData data = GetData();
//             // if (ShouldDrawShadow(data))
//             // {
//             //     drawShadow(b);
//             // }

//             // float num = tilesHigh.Value * 64;
//             // float num2 = num;
//             // if (data != null)
//             // {
//             //     num2 -= data.SortTileOffset * 64f;
//             // }

//             // num2 /= 10000f;
//             // var vector = new Vector2(tileX.Value * 64, tileY.Value * 64 + tilesHigh.Value * 64);
//             // var vector2 = Vector2.Zero;
//             // if (data != null)
//             // {
//             //     vector2 = data.DrawOffset * 4f;
//             // }

//             Microsoft.Xna.Framework.Rectangle sourceRect = getSourceRect();
//             Vector2 vector3 = new Vector2(0f, sourceRect.Height);
//             b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, vector + vector2), sourceRect, color * alpha, 0f, vector3, 4f, SpriteEffects.None, num2);
//             if (magical.Value && buildingType.Value.Equals("Gold Clock"))
//             {
//                 if (Game1.netWorldState.Value.goldenClocksTurnedOff.Value)
//                 {
//                     b.Draw(Game1.mouseCursors_1_6, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 68, tileY.Value * 64 - 56)), new Microsoft.Xna.Framework.Rectangle(498, 368, 13, 9), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)((tileY.Value + tilesHigh.Value) * 64) / 10000f + 0.0001f);
//                 }
//                 else
//                 {
//                     b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 92, tileY.Value * 64 - 40)), Town.hourHandSource, Color.White * alpha, (float)(Math.PI * 2.0 * (double)((float)(Game1.timeOfDay % 1200) / 1200f) + (double)((float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes / 23f)), new Vector2(2.5f, 8f), 3f, SpriteEffects.None, (float)((tileY.Value + tilesHigh.Value) * 64) / 10000f + 0.0001f);
//                     b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 92, tileY.Value * 64 - 40)), Town.minuteHandSource, Color.White * alpha, (float)(Math.PI * 2.0 * (double)((float)(Game1.timeOfDay % 1000 % 100 % 60) / 60f) + (double)((float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes * 1.02f)), new Vector2(2.5f, 12f), 3f, SpriteEffects.None, (float)((tileY.Value + tilesHigh.Value) * 64) / 10000f + 0.00011f);
//                     b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 92, tileY.Value * 64 - 40)), Town.clockNub, Color.White * alpha, 0f, new Vector2(2f, 2f), 4f, SpriteEffects.None, (float)((tileY.Value + tilesHigh.Value) * 64) / 10000f + 0.00012f);
//                 }
//             }

//             if (data != null)
//             {
//                 foreach (var chest in buildingChests)
//                 {
//                     var buildingChestData = GetBuildingChestData(data, buildingChest2.Name);
//                     if (buildingChestData.DisplayTile.X != -1f && buildingChestData.DisplayTile.Y != -1f && buildingChest2.Items.Count > 0 && buildingChest2.Items[0] != null)
//                     {
//                         num2 = ((float)tileY.Value + buildingChestData.DisplayTile.Y + 1f) * 64f;
//                         num2 += 1f;
//                         float num3 = 4f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2) - buildingChestData.DisplayHeight * 64f;
//                         float num4 = ((float)tileX.Value + buildingChestData.DisplayTile.X) * 64f;
//                         float num5 = ((float)tileY.Value + buildingChestData.DisplayTile.Y - 1f) * 64f;
//                         b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(num4, num5 + num3)), new Microsoft.Xna.Framework.Rectangle(141, 465, 20, 24), Color.White * 0.75f, 0f, Vector2.Zero, 4f, SpriteEffects.None, num2 / 10000f);
//                         ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(buildingChest2.Items[0].QualifiedItemId);
//                         b.Draw(dataOrErrorItem.GetTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(num4 + 32f + 4f, num5 + 32f + num3)), dataOrErrorItem.GetSourceRect(), Color.White * 0.75f, 0f, new Vector2(8f, 8f), 4f, SpriteEffects.None, (num2 + 1f) / 10000f);
//                     }
//                 }

//                 if (data.DrawLayers != null)
//                 {
//                     foreach (BuildingDrawLayer drawLayer in data.DrawLayers)
//                     {
//                         if (drawLayer.DrawInBackground)
//                         {
//                             continue;
//                         }

//                         if (drawLayer.OnlyDrawIfChestHasContents != null)
//                         {
//                             Chest buildingChest = GetBuildingChest(drawLayer.OnlyDrawIfChestHasContents);
//                             if (buildingChest == null || buildingChest.isEmpty())
//                             {
//                                 continue;
//                             }
//                         }

//                         num2 = num - drawLayer.SortTileOffset * 64f;
//                         num2 += 1f;
//                         num2 /= 10000f;
//                         Microsoft.Xna.Framework.Rectangle sourceRect2 = drawLayer.GetSourceRect((int)Game1.currentGameTime.TotalGameTime.TotalMilliseconds);
//                         sourceRect2 = ApplySourceRectOffsets(sourceRect2);
//                         vector2 = Vector2.Zero;
//                         if (drawLayer.AnimalDoorOffset != Point.Zero)
//                         {
//                             vector2 = new Vector2((float)drawLayer.AnimalDoorOffset.X * animalDoorOpenAmount.Value, (float)drawLayer.AnimalDoorOffset.Y * animalDoorOpenAmount.Value);
//                         }

//                         Texture2D texture2D = texture.Value;
//                         if (drawLayer.Texture != null)
//                         {
//                             texture2D = Game1.content.Load<Texture2D>(drawLayer.Texture);
//                         }

//                         b.Draw(texture2D, Game1.GlobalToLocal(Game1.viewport, vector + (vector2 - vector3 + drawLayer.DrawPosition) * 4f), sourceRect2, color * alpha, 0f, new Vector2(0f, 0f), 4f, SpriteEffects.None, num2);
//                     }
//                 }
//             }

//             if (daysUntilUpgrade.Value <= 0)
//             {
//                 return;
//             }

//             if (data != null)
//             {
//                 if (data.UpgradeSignTile.X >= 0f)
//                 {
//                     num2 = ((float)tileY.Value + data.UpgradeSignTile.Y + 1f) * 64f;
//                     num2 += 2f;
//                     num2 /= 10000f;
//                     b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, getUpgradeSignLocation()), new Microsoft.Xna.Framework.Rectangle(367, 309, 16, 15), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, num2);
//                 }
//             }
//             else if (GetIndoors() is Shed)
//             {
//                 b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, getUpgradeSignLocation()), new Microsoft.Xna.Framework.Rectangle(367, 309, 16, 15), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)((tileY.Value + tilesHigh.Value) * 64) / 10000f + 0.0001f);
//             }
//         }
//     }
// }
