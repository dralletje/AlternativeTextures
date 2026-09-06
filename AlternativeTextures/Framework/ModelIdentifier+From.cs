using System.Collections.Generic;
using Incubator;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace AlternativeTextures.Framework;

public static class ModelIdentifier_From
{
    extension(ModelIdentifier modelIdentifier)
    {
        public static ModelIdentifier? From(WorldObject worldObject) =>
            worldObject switch
            {
                WorldObject.Object(var @object) => From(@object),
                WorldObject.TerrainFeature(var terrainFeature) => From(terrainFeature),
                WorldObject.Floor decoration => ModelIdentifier.Floor,
                WorldObject.Wallpaper decoration => ModelIdentifier.Wallpaper,
                WorldObject.Mailbox => ModelIdentifier.Mailbox,
                /// TODO There was an exception to make the Tractor mod work too, lets test if that exception is still necessary
                WorldObject.Building({ buildingType.Value: "Farmhouse" }) => TextureType.Building.WithName(
                    $"Farmhouse_{Game1.MasterPlayer.HouseUpgradeLevel}"
                ),
                WorldObject.Building(var building) => TextureType.Building.WithName(building.buildingType.Value),
            };

        public static ModelIdentifier From(Building building) =>
            building switch
            {
                { buildingType.Value: "Farmhouse" } => TextureType.Building.WithName(
                    $"Farmhouse_{Game1.MasterPlayer.HouseUpgradeLevel}"
                ),
                _ => TextureType.Building.WithName(building.buildingType.Value),
            };

        public static ModelIdentifier From(StardewValley.Object obj) =>
            obj switch
            {
                { bigCraftable.Value: true } bigcraftable => new()
                {
                    Type = TextureType.Craftable,
                    IsName = true,
                    String = Game1.bigCraftableData.GetValueOrNull(bigcraftable.ItemId)?.Name ?? obj.name,
                },
                Furniture furniture => new()
                {
                    Type = TextureType.Furniture,
                    IsName = true,
                    String =
                        Game1
                            .content.Load<Dictionary<string, string>>("Data\\Furniture")
                            .GetValueOrNull(furniture.ItemId)
                            ?.Split('/')[0]
                        ?? obj.name,
                },
                { } anything => new()
                {
                    Type = TextureType.Craftable,
                    IsName = true,
                    String = Game1.objectData.GetValueOrNull(anything.ItemId)?.Name ?? obj.name,
                },
            };

        public static ModelIdentifier? From(TerrainFeature terrainFeature) =>
            terrainFeature switch
            {
                GiantCrop giantCrop => TextureType.GiantCrop.WithName(giantCrop.InternalName!),

                ResourceClump clump => clump.parentSheetIndex.Value switch
                {
                    /// TODO Add some more? There are more defined like this in code
                    ResourceClump.stumpIndex => TextureType.ResourceClump.WithName("Stump"),
                    ResourceClump.hollowLogIndex => TextureType.ResourceClump.WithName("Log"),
                    ResourceClump.meteoriteIndex => TextureType.ResourceClump.WithName("Meteor"),
                    ResourceClump.boulderIndex => TextureType.ResourceClump.WithName("Boulder"),
                    /// TODO Find extra names in game data somehow?
                    /// .... Need to figure out what happens if people add custom ResourceClumps using Content Patcher
                    _ => null,
                },

                Flooring flooring => TextureType.Flooring.WithName(flooring.InternalName),

                /// This could be just a tenerary, but it got formatter very odd, so it's a switch now
                HoeDirt hoeDirt when hoeDirt.crop is not null => (
                    Game1.objectData.GetValueOrNull(hoeDirt.crop.netSeedIndex.Value)?.Name is { } name
                        ? TextureType.Crop.WithName(name)
                        : null
                ),

                Grass grass => TextureType.Grass.WithName("Grass"),

                Bush bush => bush.size.Value switch
                {
                    0 => TextureType.Bush.WithName("Small"),
                    1 => TextureType.Bush.WithName(bush.townBush.Value ? "Town" : "Medium"),
                    2 => TextureType.Bush.WithName("Large"),
                    3 => TextureType.Bush.WithName("Tea"),
                    4 => TextureType.Bush.WithName("Walnut"),
                    _ => null,
                },
                Tree tree => TextureType.Tree.WithName(
                    tree.treeType.Value switch
                      {
                          Tree.bushyTree => "Oak",
                          Tree.leafyTree => "Maple",
                          Tree.pineTree => "Pine",
                          Tree.mahoganyTree => "Mahogany",
                          Tree.mushroomTree => "Mushroom",
                          Tree.palmTree => "Palm_1",
                          Tree.palmTree2 => "Palm_2",
                          _ => tree.treeType.Value,
                      }
                ),

                /// Previously the code checked `Game1.fruitTreeData.ContainsKey(fruitTree.treeId.Value)` first... Not sure why
                FruitTree fruitTree => (
                    Game1.fruitTreeData.ContainsKey(fruitTree.treeId.Value)
                    && Game1.objectData.GetValueOrNull(fruitTree.treeId.Value)?.Name is { } name
                        ? TextureType.FruitTree.WithName(name)
                        : null
                ),

                /// DRAL TODO Distinguish between terrainFeature != null and no terrain at all?
                { } unknown => null,
                null => null,
            };
    }
}
