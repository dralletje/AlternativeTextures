using System;
using System.Linq;
using AlternativeTextures.Framework;
using AlternativeTextures.Stardew;
using Incubator;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;

namespace AlternativeTextures.Tools;

class SprayCanTool : ICustomTool
{
    internal const string SPRAY_CAN_FLAG = AlternativeTextures.SPRAY_CAN_FLAG;
    internal const string SPRAY_CAN_RADIUS = AlternativeTextures.SPRAY_CAN_RADIUS;
    internal const string ENABLED_SPRAY_CAN_TEXTURES = AlternativeTextures.ENABLED_SPRAY_CAN_TEXTURES;

    private readonly GenericTool tool;

    private SprayCanTool(GenericTool tool)
    {
        this.tool = tool;
    }

    public IDisposable? Start()
    {
        return null;
    }

    public bool OnButtonPressed(ButtonPressedEventArgs e)
    {
        if (e.Button is SButton.MouseRight)
        {
            var tile = Game1.player.ActiveTargetTile;
            return OpenMenu(tile);
        }
        else
        {
            return e.Button is SButton.MouseLeft && false;
            // LeftClickSprayCan(tool, xTile, yTile)
        }
    }

    private bool OpenMenu(Tile tile)
    {
        if (IPaintable.OnTile(tile).FirstOrDefault() is { } paintable)
        {
            if (paintable.Type != tool.modData.GetValueOrNull(SPRAY_CAN_FLAG))
            {
                Game1.player.modData[ENABLED_SPRAY_CAN_TEXTURES] = null;
            }
            tool.modData[SPRAY_CAN_FLAG] = paintable.Type;
            return true;
        }
        else
        {
            return false;
        }
    }

    // private void Apply()
    // {

    //   // if (_lastSprayCanTile.X == xTile && _lastSprayCanTile.Y == yTile)
    //   // {
    //   //     return;
    //   // }
    //   // _lastSprayCanTile = new Point(xTile, yTile);

    //   if (Game1.player.modData.ContainsKey(ENABLED_SPRAY_CAN_TEXTURES) is false || string.IsNullOrEmpty(Game1.player.modData[ENABLED_SPRAY_CAN_TEXTURES]))
    //   {
    //     Game1.addHUDMessage(new HUDMessage(AlternativeTextures.modHelper.Translation.Get("messages.warning.spray_can_is_empty"), 3) { timeLeft = 2000 });
    //   }
    //   else
    //   {
    //     var selectedModelsToVariations = JsonConvert.DeserializeObject<Dictionary<string, SelectedTextureModel>>(Game1.player.modData[ENABLED_SPRAY_CAN_TEXTURES]);

    //     if (selectedModelsToVariations.Count == 0)
    //     {
    //       Game1.addHUDMessage(new HUDMessage(AlternativeTextures.modHelper.Translation.Get("messages.warning.spray_can_is_empty"), 3) { timeLeft = 2000 });
    //       return;
    //     }

    //     int tileRadius = 1;
    //     if (Game1.player.modData.ContainsKey(SPRAY_CAN_RADIUS) is false || int.TryParse(Game1.player.modData[SPRAY_CAN_RADIUS], out tileRadius) is false)
    //     {
    //       Game1.player.modData[SPRAY_CAN_RADIUS] = "1";
    //     }
    //     tileRadius = tileRadius > 0 ? tileRadius - 1 : tileRadius;

    //     // Convert to standard game tiles
    //     xTile /= 64;
    //     yTile /= 64;
    //     for (int x = xTile - tileRadius; x <= xTile + tileRadius; x++)
    //     {
    //       for (int y = yTile - tileRadius; y <= yTile + tileRadius; y++)
    //       {
    //         var actualX = x * 64;
    //         var actualY = y * 64;

    //         // Select random texture
    //         Random random = new Random(Guid.NewGuid().GetHashCode());
    //         var selectedModelIndex = random.Next(0, selectedModelsToVariations.Count);
    //         var actualSelectedModel = selectedModelsToVariations.ElementAt(selectedModelIndex).Value;
    //         var selectedVariationIndex = random.Next(0, actualSelectedModel.Variations.Count);
    //         var actualSelectedVariation = actualSelectedModel.Variations[selectedVariationIndex].ToString();

    //         // Verify that a supported object exists at the tile
    //         var resourceClump = PatchTemplate.GetResourceClumpAt(Game1.currentLocation, actualX, actualY);
    //         var terrainFeature = PatchTemplate.GetTerrainFeatureAt(Game1.currentLocation, actualX, actualY);
    //         if (resourceClump is GiantCrop giantCrop)
    //         {
    //           GiantCropPatch.TryGetGiantCropName(giantCrop, out string instanceName);
    //           if (tool.modData[SPRAY_CAN_FLAG] == instanceName)
    //           {
    //             giantCrop.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = actualSelectedModel.Owner;
    //             giantCrop.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = actualSelectedModel.TextureName;
    //             giantCrop.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = actualSelectedVariation;
    //             continue;
    //           }
    //         }
    //         else if (terrainFeature is Flooring flooring)
    //         {
    //           var modelType = TextureType.Flooring;
    //           if (tool.modData[SPRAY_CAN_FLAG] == $"{modelType}_{PatchTemplate.GetFlooringName(flooring)}")
    //           {
    //             flooring.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = actualSelectedModel.Owner;
    //             flooring.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = actualSelectedModel.TextureName;
    //             flooring.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = actualSelectedVariation;
    //             continue;
    //           }
    //         }
    //         if (terrainFeature is HoeDirt hoeDirt && hoeDirt.crop is not null)
    //         {
    //           var modelType = TextureType.Crop;
    //           var instanceName = Game1.objectData.ContainsKey(hoeDirt.crop.netSeedIndex.Value) ? Game1.objectData[hoeDirt.crop.netSeedIndex.Value].Name : String.Empty;
    //           if (tool.modData[SPRAY_CAN_FLAG] == $"{modelType}_{instanceName}")
    //           {
    //             hoeDirt.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = actualSelectedModel.Owner;
    //             hoeDirt.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = actualSelectedModel.TextureName;
    //             hoeDirt.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = actualSelectedVariation;
    //             continue;
    //           }
    //         }
    //         if (terrainFeature is Grass grass)
    //         {
    //           var modelType = TextureType.Grass;
    //           if (tool.modData[SPRAY_CAN_FLAG] == $"{modelType}_Grass")
    //           {
    //             grass.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = actualSelectedModel.Owner;
    //             grass.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = actualSelectedModel.TextureName;
    //             grass.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = actualSelectedVariation;
    //             continue;
    //           }
    //         }
    //         if (terrainFeature is Tree tree)
    //         {
    //           var modelType = TextureType.Tree;
    //           if (tool.modData[SPRAY_CAN_FLAG] == $"{modelType}_{PatchTemplate.GetTreeTypeString(tree)}")
    //           {
    //             tree.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = actualSelectedModel.Owner;
    //             tree.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = actualSelectedModel.TextureName;
    //             tree.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = actualSelectedVariation;
    //             continue;
    //           }
    //           else
    //           {
    //             Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.warning.invalid_copied_texture", new { textureName = tool.modData[SPRAY_CAN_FLAG] }), 3) { timeLeft = 2000 });
    //           }
    //         }
    //         if (terrainFeature is FruitTree fruitTree)
    //         {
    //           var modelType = TextureType.FruitTree;
    //           var saplingName = Game1.fruitTreeData.ContainsKey(fruitTree.treeId.Value) ? Game1.objectData[fruitTree.treeId.Value].Name : String.Empty;
    //           if (tool.modData[SPRAY_CAN_FLAG] == $"{modelType}_{saplingName}")
    //           {
    //             fruitTree.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = actualSelectedModel.Owner;
    //             fruitTree.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = actualSelectedModel.TextureName;
    //             fruitTree.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = actualSelectedVariation;
    //             continue;
    //           }
    //           else
    //           {
    //             Game1.addHUDMessage(new HUDMessage(AlternativeTextures.modHelper.Translation.Get("messages.warning.invalid_copied_texture", new { textureName = tool.modData[SPRAY_CAN_FLAG] }), 3) { timeLeft = 2000 });
    //           }
    //         }

    //         var placedObject = PatchTemplate.GetObjectAt(Game1.currentLocation, actualX, actualY);
    //         if (placedObject is not null)
    //         {
    //           var modelType = placedObject is Furniture ? TextureType.Furniture : TextureType.Craftable;
    //           if (tool.modData[SPRAY_CAN_FLAG] == $"{modelType}_{PatchTemplate.GetObjectName(placedObject)}")
    //           {
    //             placedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = actualSelectedModel.Owner;
    //             placedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = actualSelectedModel.TextureName;
    //             placedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = actualSelectedVariation;
    //             continue;
    //           }
    //         }
    //       }
    //     }
    //   }
    // }

    public static SprayCanTool? From(IModHelper helper, Tool? tool)
    {
        if (tool is GenericTool genericTool)
        {
            return tool.modData.ContainsKey(AlternativeTextures.SPRAY_CAN_FLAG) ? new SprayCanTool(genericTool) : null;
        }
        else
        {
            return null;
        }
    }
}
