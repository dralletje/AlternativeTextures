using System;
using System.Collections.Generic;
using AlternativeTextures.PatchDrawMod.Patches;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;

namespace AlternativeTextures.Framework;

static class PaintableExtensions
{
  public static bool IsPositionNearMailbox(GameLocation location, Point mailboxPosition, int x, int y)
  {
    var isNearMailbox = (mailboxPosition.X == x) && (mailboxPosition.Y == y || mailboxPosition.Y == y + 1);
    return isNearMailbox;
  }
}

// static class Test
// {
// public static bool IsPositionNearMailbox(GameLocation location, Point mailboxPosition, int x, int y)
//     {
//         var isNearMailbox = (mailboxPosition.X == x) && (mailboxPosition.Y == y || mailboxPosition.Y == y + 1);
//         return isNearMailbox;
//     }

//     public static Building? GetBuildingAt(GameLocation location, Tile tile)
//     {
//         if (!location.IsBuildableLocation())
//             return null;

//         var targetedBuilding = location.getBuildingAt(tile.ToVector2());
//         Console.Log($"targetedBuilding.buildingType: {targetedBuilding.buildingType}");
//         var isFarmerHouse = false;

//         if (location is not Farm farm) return null;

//         var farmerHouse = farm.GetMainFarmHouse();

//         // Check for mailbox
//         var mailboxPosition = farm.GetMainMailboxPosition();
//         if (IsPositionNearMailbox(location, mailboxPosition, tile.X, tile.Y))
//         {
//             // var modelType = TextureType.Building;
//             // var usedSecondaryTile =
//             //     string.IsNullOrEmpty(location.doesTileHaveProperty(tile.X, tile.Y, "Action", "Buildings"))
//             //     && location.doesTileHaveProperty(tile.X, tile.Y + 1, "Action", "Buildings") == "Mailbox";
//             // var mailboxObj = new Object("100", 1, isRecipe: false, -1)
//             // {
//             //     TileLocation = new Vector2(x / 64, (y + (usedSecondaryTile ? 64 : 0)) / 64),
//             // };

//             // foreach (string key in location.modData.Keys)
//             // {
//             //     mailboxObj.modData[key] = location.modData[key];
//             // }

//             // var modelName = mailboxObj
//             //     .modData["AlternativeTextureName.Mailbox"]
//             //     .Replace($"{mailboxObj.modData["AlternativeTextureOwner.Mailbox"]}.", String.Empty);
//             // if (
//             //     mailboxObj.modData.ContainsKey("AlternativeTextureSeason.Mailbox")
//             //     && !String.IsNullOrEmpty(mailboxObj.modData["AlternativeTextureSeason.Mailbox"])
//             // )
//             // {
//             //     modelName = GetModelNameWithoutSeason(
//             //         modelName,
//             //         mailboxObj.modData["AlternativeTextureSeason.Mailbox"]
//             //     );
//             // }
//         }

//         if (farmerHouse == targetedBuilding)
//         {
//             isFarmerHouse = true;

//             targetedBuilding = new Building();
//             targetedBuilding.buildingType.Value = $"Farmhouse_{Game1.MasterPlayer.HouseUpgradeLevel}";
//             targetedBuilding.tileX.Value = farmerHouse.tileX.Value;
//             targetedBuilding.tileY.Value = farmerHouse.tileY.Value;
//             targetedBuilding.tilesWide.Value = farmerHouse.tilesWide.Value;
//             targetedBuilding.tilesHigh.Value = farmerHouse.tilesHigh.Value;

//             var modelType = TextureType.Building;
//             if (
//                 !farm.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME)
//                 || !farm.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME].Contains(targetedBuilding.buildingType.Value)
//             )
//             {
//                 var instanceSeasonName =
//                     $"{modelType}_{targetedBuilding.buildingType.Value}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
//                 AssignDefaultModData(farm, instanceSeasonName, true);
//             }

//             foreach (string key in farm.modData.Keys)
//             {
//                 targetedBuilding.modData[key] = farm.modData[key];
//             }
//         }
//     }

//         if (
//             AlternativeTextures
//                 .textureManager.GetAvailableTextureModels(modelName, Game1.GetSeasonForLocation(Game1.currentLocation))
//                 .Count == 0
//         )
//         {
//             if (
//                 targetedBuilding.GetData() is var data
//                 && data is not null
//                 && data.Skins is not null
//                 && data.Skins.Count > 0
//             )
//             {
//                 // Skip no texture warning
//             }
//             else
//             {
//                 Game1.addHUDMessage(
//                     new HUDMessage(
//                         _helper.Translation.Get(
//                             "messages.warning.no_textures_for_season",
//                             new { itemName = modelName }
//                         ),
//                         3
//                     )
//                 );
//                 return CancelUsing(who);
//             }
//         }

//         // Verify this building has a texture we can target
//         if (isFarmerHouse is false)
//         {
//             var texturePath = PathUtilities.NormalizePath(Path.Combine(targetedBuilding.textureName() + ".png"));
//             try
//             {
//                 _ = _helper.GameContent.Load<Texture2D>(Path.Combine(targetedBuilding.textureName()));
//                 _monitor.Log($"{modelName} has a targetable texture within Buildings: {texturePath}", LogLevel.Trace);
//             }
//             catch (ContentLoadException ex)
//             {
//                 Game1.addHUDMessage(
//                     new HUDMessage(
//                         AlternativeTextures.modHelper.Translation.Get(
//                             "messages.warning.custom_building_not_supported",
//                             new { itemName = modelName }
//                         ),
//                         3
//                     )
//                 );
//                 _monitor.Log($"Failed to load texture for {modelName} at the path {texturePath}: {ex}", LogLevel.Trace);
//                 return CancelUsing(who);
//             }
//         }

//         // Display texture menu
//         var buildingObj = new Object(targetedBuilding.buildingType.Value, 1, isRecipe: false, -1)
//         {
//             TileLocation = new Vector2(targetedBuilding.tileX.Value, targetedBuilding.tileY.Value),
//         };
//         buildingObj.modData.SetFromSerialization(targetedBuilding.modData);

//         Game1.activeClickableMenu = GetMenu(
//             buildingObj,
//             buildingObj.TileLocation * 64f,
//             GetTextureType(targetedBuilding),
//             modelName,
//             _helper.Translation.Get("tools.name.paint_bucket"),
//             textureTileWidth: targetedBuilding.tilesWide.Value,
//             isSprayCan: isSprayCan
//         );

//         return CancelUsing(who);
//     }
// }
