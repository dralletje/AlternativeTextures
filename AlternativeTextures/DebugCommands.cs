using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.GameData.GiantCrops;
using StardewValley.Monsters;
using StardewValley.TerrainFeatures;

namespace AlternativeTextures;

/// <summary>
/// Add commands that are not AlternativeTexture dependent, just useful to test for self
/// </summary>
/// <param name="mod"></param>
class DebugCommands(IMod mod)
{
    IMonitor Monitor = mod.Monitor;
    IModHelper Helper = mod.Helper;

    public void Register()
    {
        Helper.ConsoleCommands.Add(
            "at_spawn_monsters",
            "Spawns monster(s) of a specified type and quantity at the current location.\n\nUsage: at_spawn_monsters [MONSTER_ID] (QUANTITY)",
            this.DebugSpawnMonsters
        );
        Helper.ConsoleCommands.Add(
            "at_spawn_gc",
            "Spawns a giant crop based given harvest product id (e.g. Melon == 254).\n\nUsage: at_spawn_gc [HARVEST_ID]",
            this.DebugSpawnGiantCrop
        );
        Helper.ConsoleCommands.Add(
            "at_spawn_rc",
            "Spawns a resource clump based given resource name (e.g. Stump).\n\nUsage: at_spawn_rc [RESOURCE_NAME]",
            this.DebugSpawnResourceClump
        );
        Helper.ConsoleCommands.Add(
            "at_spawn_child",
            "Spawns a child. Potentially buggy / gamebreaking, do not use. \n\nUsage: at_spawn_child [AGE] [IS_MALE] [SKIN_TONE]",
            this.DebugSpawnChild
        );
        Helper.ConsoleCommands.Add(
            "at_set_age",
            "Sets age for all children in location. Potentially buggy / gamebreaking, do not use. \n\nUsage: at_set_age [AGE]",
            this.DebugSetAge
        );
    }

    private void DebugSpawnMonsters(string command, string[] args)
    {
        if (args.Length == 0)
        {
            Monitor.Log($"Missing required arguments: [MONSTER_ID]", LogLevel.Warn);
            return;
        }

        int amountToSpawn = 1;
        if (args.Length > 1 && int.TryParse(args[1], out amountToSpawn) is false)
        {
            Monitor.Log($"Invalid count given for (QUANTITY)", LogLevel.Warn);
            return;
        }
        Type monsterType = Type.GetType("StardewValley.Monsters." + args[0] + ",Stardew Valley");

        Monitor.Log(Game1.player.Tile.ToString(), LogLevel.Debug);
        for (int i = 0; i < amountToSpawn; i++)
        {
            var monster = Activator.CreateInstance(monsterType, new object[] { Game1.player.Tile }) as Monster;
            monster.Position = Game1.player.Position;
            Game1.currentLocation.characters.Add(monster);
        }
    }

    private void DebugSpawnGiantCrop(string command, string[] args)
    {
        if (args.Length == 0)
        {
            Monitor.Log($"Missing required arguments: [HARVEST_ID]", LogLevel.Warn);
            return;
        }

        if (
            !(Game1.currentLocation.GetData()?.CanPlantHere ?? Game1.currentLocation.IsFarm)
            || (Game1.currentLocation is not Farm && !Game1.currentLocation.HasMapPropertyWithValue("AllowGiantCrops"))
        )
        {
            Monitor.Log($"Command can only be used on a plantable location allowing giant crops.", LogLevel.Warn);
            return;
        }

        GameLocation gameLocation = Game1.currentLocation;

        foreach (var tile in gameLocation.terrainFeatures.Pairs.Where(t => t.Value is HoeDirt))
        {
            Crop crop = (tile.Value as HoeDirt).crop;

            if (crop is null || crop.indexOfHarvest.Value != args[0])
            {
                continue;
            }

            if (crop.TryGetGiantCrops(out var giantCrops))
            {
                Vector2 vector = crop.tilePosition;
                Point point = Utility.Vector2ToPoint(vector);

                foreach (KeyValuePair<string, GiantCropData> item in giantCrops)
                {
                    string key = item.Key;
                    GiantCropData value = item.Value;
                    bool flag = true;

                    for (int i = point.Y; i < point.Y + value.TileSize.Y; i++)
                    {
                        for (int j = point.X; j < point.X + value.TileSize.X; j++)
                        {
                            Vector2 key2 = new(j, i);

                            if (
                                !gameLocation.terrainFeatures.TryGetValue(key2, out TerrainFeature terrainFeature)
                                || terrainFeature is not HoeDirt hoeDirt2
                                || hoeDirt2.crop?.indexOfHarvest.Value != crop.indexOfHarvest.Value
                            )
                            {
                                flag = false;
                                break;
                            }
                        }
                        if (!flag)
                        {
                            break;
                        }
                    }
                    if (!flag)
                    {
                        continue;
                    }
                    for (int k = point.Y; k < point.Y + value.TileSize.Y; k++)
                    {
                        for (int l = point.X; l < point.X + value.TileSize.X; l++)
                        {
                            Vector2 key3 = new(l, k);

                            ((HoeDirt)gameLocation.terrainFeatures[key3]).crop = null;
                        }
                    }
                    gameLocation.resourceClumps.Add(new GiantCrop(key, vector));
                    break;
                }
            }
        }
    }

    private void DebugSpawnResourceClump(string command, string[] args)
    {
        if (args.Length == 0)
        {
            Monitor.Log($"Missing required arguments: [RESOURCE_NAME]", LogLevel.Warn);
            return;
        }

        if (!Game1.currentLocation.IsOutdoors)
        {
            Monitor.Log($"Command can only be used outdoors.", LogLevel.Warn);
            return;
        }

        if (args[0].ToLower() != "stump")
        {
            Monitor.Log($"That resource isn't supported.", LogLevel.Warn);
            return;
        }

        Game1.currentLocation.resourceClumps.Add(new ResourceClump(600, 2, 2, Game1.player.Tile + new Vector2(1, 1)));
    }

    private void DebugSpawnChild(string command, string[] args)
    {
        if (args.Length < 2)
        {
            Monitor.Log($"Missing required arguments: [AGE] [IS_MALE] [SKIN_TONE]", LogLevel.Warn);
            return;
        }

        var age = -1;
        if (!int.TryParse(args[0], out age) || age < 0)
        {
            Monitor.Log($"Invalid number given: {args[0]}", LogLevel.Warn);
            return;
        }

        var isMale = false;
        if (args[1].ToLower() == "true")
        {
            isMale = true;
        }

        var hasDarkSkin = false;
        if (args[2].ToLower() == "dark")
        {
            hasDarkSkin = true;
        }

        var child = new Child("Test", isMale, hasDarkSkin, Game1.player);
        child.Position = Game1.player.Position;
        child.Age = age;
        Game1.currentLocation.characters.Add(child);
    }

    private void DebugSetAge(string command, string[] args)
    {
        if (args.Length == 0)
        {
            Monitor.Log($"Missing required arguments: [AGE]", LogLevel.Warn);
            return;
        }

        var age = -1;
        if (!int.TryParse(args[0], out age))
        {
            Monitor.Log($"Invalid number given: {args[0]}", LogLevel.Warn);
            return;
        }

        foreach (var child in Game1.currentLocation.characters.Where(c => c is Child))
        {
            child.Age = 3;
        }
    }
}
