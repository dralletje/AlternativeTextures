using System;
using System.Collections.Generic;
using System.Linq;
using AlternativeTextures.Framework.Interfaces;
using AlternativeTextures.Framework.Managers;
using AlternativeTextures.Framework.Models;
using AlternativeTextures.Framework.Patches.Entities;
using AlternativeTextures.Framework.Utilities;
using AlternativeTextures.Framework.Utilities.Extensions;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using static AlternativeTextures.Framework.Models.AlternativeTextureModel;
using Object = StardewValley.Object;

namespace AlternativeTextures.Framework.Patches;

internal class PatchTemplate()
{
    internal static string GetModelNameWithoutSeason(string modelName, string season)
    {
        return modelName.ReplaceLastInstance($"_{season}", String.Empty);
    }

    internal static string GetObjectName(Object obj)
    {
        // Perform separate check for DGA objects, before using check for vanilla objects
        if (
            IsDGAUsed()
            && AlternativeTextures.apiManager.GetDynamicGameAssetsApi() is IDynamicGameAssetsApi api
            && api != null
        )
        {
            var dgaId = api.GetDGAItemId(obj);
            if (dgaId != null)
            {
                return dgaId;
            }
        }

        if (obj.bigCraftable.Value)
        {
            return !Game1.bigCraftableData.ContainsKey(obj.ItemId) ? obj.name : Game1.bigCraftableData[obj.ItemId].Name;
        }
        else if (obj is Furniture)
        {
            var dataSheet = Game1.content.Load<Dictionary<string, string>>("Data\\Furniture");
            return !dataSheet.ContainsKey(obj.ItemId) ? obj.name : dataSheet[obj.ItemId].Split('/')[0];
        }
        else
        {
            return !Game1.objectData.ContainsKey(obj.ItemId) ? obj.name : Game1.objectData[obj.ItemId].Name;
        }
    }

    internal static string GetCharacterName(Character character)
    {
        if (character is Child child)
        {
            return child.Age >= 3
                ? $"{CharacterPatch.TODDLER_NAME_PREFIX}_{(child.Gender == 0 ? "Male" : "Female")}_{(child.darkSkinned.Value ? "Dark" : "Light")}"
                : $"{CharacterPatch.BABY_NAME_PREFIX}_{(child.darkSkinned.Value ? "Dark" : "Light")}";
        }

        if (character is FarmAnimal animal)
        {
            var animalName = animal.type.Value;
            if (animal.isBaby())
            {
                animalName = "Baby" + (animal.type.Value.Equals("Duck") ? "White Chicken" : animal.type.Value);
            }
            else if (
                animal.GetAnimalData() is not null
                && string.IsNullOrEmpty(animal.GetAnimalData().HarvestedTexture) is false
                && string.IsNullOrEmpty(animal.currentProduce.Value) is true
            )
            {
                animalName = "Sheared" + animalName;
            }
            return animalName;
        }

        if (character is Horse horse)
        {
            // Tractor mod compatibility: -794739 is the ID used by Tractor Mod for determining if a Stable is really a garage
            return horse.modData.ContainsKey("Pathoschild.TractorMod") ? "Tractor" : "Horse";
        }

        return character is Pet pet ? pet.petType.Value : character.Name;
    }

    internal static string GetBuildingName(Building building)
    {
        // Tractor mod compatibility: -794739 is the ID used by Tractor Mod for determining if a Stable is really a garage
        if (building.maxOccupants.Value == -794739)
        {
            return "Tractor Garage";
        }
        else if (building.buildingType.Value == "Farmhouse")
        {
            return $"{building.buildingType.Value}_{Game1.MasterPlayer.HouseUpgradeLevel}";
        }

        return building.buildingType.Value;
    }

    internal static Object GetObjectAt(GameLocation location, int x, int y)
    {
        // If object is furniture and currently has something on top of it, check that instead
        foreach (var furniture in location.furniture.Where(c => c.heldObject.Value != null))
        {
            if (furniture.boundingBox.Value.Contains(x, y))
            {
                return furniture.heldObject.Value;
            }
        }

        // Prioritize checking non-rug furniture first
        foreach (var furniture in location.furniture.Where(c => c.furniture_type.Value != Furniture.rug))
        {
            if (furniture.boundingBox.Value.Contains(x, y))
            {
                return furniture;
            }
        }

        // Replicating GameLocation.getObjectAt, but doing objects before rugs
        // Doing this so the object on top of rugs are given instead of the latter
        var tile = new Vector2(x / 64, y / 64);
        return location.objects.ContainsKey(tile) ? location.objects[tile] : location.getObjectAt(x, y);
    }

    internal static Building? GetBuildingAt(GameLocation location, int x, int y)
    {
        var tile = new Vector2(x / 64, y / 64);
        return location.buildings.FirstOrDefault(b => b.occupiesTile(tile)) is Building building && building != null
            ? building
            : null;
    }

    internal static TerrainFeature? GetTerrainFeatureAt(GameLocation location, int x, int y)
    {
        var tile = new Vector2(x / 64, y / 64);
        if (!location.terrainFeatures.ContainsKey(tile))
        {
            return location.largeTerrainFeatures is not null
                ? location.largeTerrainFeatures.FirstOrDefault(t => t is not null && t.Tile == tile)
                : (TerrainFeature?)null;
        }

        return location.terrainFeatures[tile];
    }

    internal static ResourceClump? GetResourceClumpAt(GameLocation location, int x, int y)
    {
        Vector2 tile = new Vector2(x / 64, y / 64);
        return !location.resourceClumps.Any(r => r.occupiesTile((int)tile.X, (int)tile.Y))
            ? null
            : location.resourceClumps.First(r => r.occupiesTile((int)tile.X, (int)tile.Y));
    }

    internal static Character GetCharacterAt(GameLocation location, int x, int y)
    {
        var tileLocation = new Vector2(x / 64, y / 64);
        var rectangle = new Rectangle(x, y, 64, 64);
        if (location.IsBuildableLocation())
        {
            foreach (var animal in location.animals.Values)
            {
                if (animal.GetBoundingBox().Intersects(rectangle))
                {
                    return animal;
                }
            }
        }
        if (location is AnimalHouse animalHouse)
        {
            foreach (var animal in animalHouse.animals.Values)
            {
                if (animal.GetBoundingBox().Intersects(rectangle))
                {
                    return animal;
                }
            }
        }

        foreach (var specialCharacter in location.characters.Where(c => c is Horse or Pet))
        {
            if (specialCharacter is Horse horse && horse.GetBoundingBox().Intersects(rectangle))
            {
                return horse;
            }

            if (specialCharacter is Pet pet && pet.GetBoundingBox().Intersects(rectangle))
            {
                return pet;
            }
        }

        return location.isCharacterAtTile(tileLocation);
    }

    internal static string GetFlooringName(Flooring floor)
    {
        return ItemRegistry.GetData(floor.GetData()?.ItemId)?.InternalName ?? string.Empty;
    }

    internal static string GetTreeTypeString(Tree tree)
    {
        return tree.treeType.Value switch
        {
            Tree.bushyTree => "Oak",
            Tree.leafyTree => "Maple",
            Tree.pineTree => "Pine",
            Tree.mahoganyTree => "Mahogany",
            Tree.mushroomTree => "Mushroom",
            Tree.palmTree => "Palm_1",
            Tree.palmTree2 => "Palm_2",
            _ => tree.treeType.Value,
        };
    }

    internal static string GetBushTypeString(Bush bush)
    {
        return bush.size.Value switch
        {
            0 => "Small",
            1 => bush.townBush.Value ? "Town" : "Medium",
            2 => "Large",
            3 => "Tea",
            4 => "Walnut",
            _ => String.Empty,
        };
    }

    internal static TextureType GetTextureType(object obj)
    {
        return obj switch
        {
            Character character => TextureType.Character,
            Flooring floor => TextureType.Flooring,
            Tree tree => TextureType.Tree,
            FruitTree fruitTree => TextureType.FruitTree,
            Grass grass => TextureType.Grass,
            Bush bush => TextureType.Bush,
            ResourceClump resourceClump => TextureType.GiantCrop,
            TerrainFeature hoeDirt => TextureType.Crop,
            Building building => TextureType.Building,
            Furniture furniture => TextureType.Furniture,
            Object craftable => TextureType.Craftable,
            DecoratableLocation location => TextureType.Decoration,
            _ => TextureType.Unknown,
        };
    }

    internal static bool HasCachedTextureName<T>(T type, bool probe = false)
    {
        if (type is Object obj && obj.modData.ContainsKey("AlternativeTextureNameCached"))
        {
            if (!probe)
            {
                obj.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = obj.modData["AlternativeTextureNameCached"];
                obj.modData.Remove("AlternativeTextureNameCached");
            }

            return true;
        }

        return false;
    }

    internal static bool IsPositionNearMailbox(GameLocation location, Point mailboxPosition, int x, int y)
    {
        var isNearMailbox = (mailboxPosition.X == x) && (mailboxPosition.Y == y || mailboxPosition.Y == y + 1);
        return isNearMailbox;
    }

    internal static bool IsDGAUsed()
    {
        return AlternativeTextures.modHelper.ModRegistry.IsLoaded("spacechase0.DynamicGameAssets");
    }

    internal static bool IsSolidFoundationsUsed()
    {
        return AlternativeTextures.modHelper.ModRegistry.IsLoaded("PeacefulEnd.SolidFoundations");
    }

    internal static bool IsDGAObject(object obj)
    {
        if (
            IsDGAUsed()
            && AlternativeTextures.apiManager.GetDynamicGameAssetsApi() is IDynamicGameAssetsApi api
            && api != null
        )
        {
            var dgaId = api.GetDGAItemId(obj);
            if (dgaId != null)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsTextureRandomnessEnabled<T>(T type)
    {
        switch (type)
        {
            case Flooring:
                return AlternativeTextures.modConfig.UseRandomTexturesWhenPlacingFlooring;
            case FruitTree:
                return AlternativeTextures.modConfig.UseRandomTexturesWhenPlacingFruitTree;
            case Tree:
                return AlternativeTextures.modConfig.UseRandomTexturesWhenPlacingTree;
            case HoeDirt:
                return AlternativeTextures.modConfig.UseRandomTexturesWhenPlacingHoeDirt;
            case Grass:
                return AlternativeTextures.modConfig.UseRandomTexturesWhenPlacingGrass;
            case Furniture:
                return AlternativeTextures.modConfig.UseRandomTexturesWhenPlacingFurniture;
            case Object obj:
                if (obj is not null && obj.Name == "Artifact Spot")
                {
                    return AlternativeTextures.modConfig.UseRandomTexturesWhenSpawningArtifactSpots;
                }
                return AlternativeTextures.modConfig.UseRandomTexturesWhenPlacingObject;
            case FarmAnimal:
                return AlternativeTextures.modConfig.UseRandomTexturesWhenPlacingFarmAnimal;
            case Monster:
                return AlternativeTextures.modConfig.UseRandomTexturesWhenPlacingMonster;
            case Building:
                return AlternativeTextures.modConfig.UseRandomTexturesWhenPlacingBuilding;
        }

        return true;
    }

    internal static bool AssignDefaultModData<T>(
        T type,
        string modelName,
        bool trackSeason = false,
        bool trackSheetId = false
    )
    {
        if (HasCachedTextureName(type))
        {
            return false;
        }

        var textureModel = new AlternativeTextureModel()
        {
            DisplayName = "",
            Texture = null!,
            ForModel = ModelIdentifier.FromString(modelName)!,
            TextureWidth = 16,
            TextureHeight = 16,
            Variation = 1,
            PackManifest = AlternativeTextures.modManifest,
            Season = Game1.GetSeasonForLocation(Game1.currentLocation),
        };
        switch (type)
        {
            case Object obj:
                AssignObjectModData(obj, modelName, textureModel, -1, trackSeason, trackSheetId);
                return true;
            case TerrainFeature terrain:
                AssignTerrainFeatureModData(terrain, modelName, textureModel, -1, trackSeason);
                return true;
            case Character character:
                AssignCharacterModData(character, modelName, textureModel, -1, trackSeason);
                return true;
            case Building building:
                AssignBuildingModData(building, modelName, textureModel, -1, trackSeason);
                return true;
            case DecoratableLocation decoratableLocation:
                AssignDecoratableLocationModData(decoratableLocation, modelName, textureModel, -1, trackSeason);
                return true;
            case GameLocation gameLocation when gameLocation.IsBuildableLocation():
                AssignGameLocationModData(gameLocation, modelName, textureModel, -1, trackSeason);
                return true;
        }

        return false;
    }

    internal static bool AssignModData<T>(T type, string modelName, bool trackSeason = false, bool trackSheetId = false)
    {
        return false;

        /// IMMEDIATE TODO
        // if (HasCachedTextureName(type) || IsTextureRandomnessEnabled(type) is false)
        // {
        //     return false;
        // }

        // var textureModel = AlternativeTextures.textureManager.GetRandomTextureModel(modelName);

        // var selectedVariation = Game1.random.Next(-1, textureModel.Variations);
        // if (textureModel.DefaultVariation is not null)
        // {
        //     selectedVariation = textureModel.DefaultVariation.Value;
        // }
        // else if (textureModel.ManualVariations.Count > 0)
        // {
        //     var weightedSelection = textureModel
        //         .ManualVariations.Where(v => v.ChanceWeight > Game1.random.NextDouble())
        //         .ToList();
        //     if (weightedSelection.Count > 0)
        //     {
        //         var randomWeightedSelection = Game1.random.Next(
        //             !textureModel.ManualVariations.Any(v => v.Id == -1) ? -1 : 0,
        //             weightedSelection.Count
        //         );
        //         selectedVariation = randomWeightedSelection == -1 ? -1 : weightedSelection[randomWeightedSelection].Id;
        //     }
        //     else
        //     {
        //         return AssignDefaultModData<T>(type, modelName, trackSeason, trackSheetId);
        //     }
        // }

        // switch (type)
        // {
        //     case Object obj:
        //         AssignObjectModData(obj, modelName, textureModel, selectedVariation, trackSeason, trackSheetId);
        //         return true;
        //     case TerrainFeature terrain:
        //         AssignTerrainFeatureModData(terrain, modelName, textureModel, selectedVariation, trackSeason);
        //         return true;
        //     case Character character:
        //         AssignCharacterModData(character, modelName, textureModel, selectedVariation, trackSeason);
        //         return true;
        //     case Building building:
        //         AssignBuildingModData(building, modelName, textureModel, selectedVariation, trackSeason);
        //         return true;
        //     case DecoratableLocation decoratableLocation:
        //         AssignDecoratableLocationModData(
        //             decoratableLocation,
        //             modelName,
        //             textureModel,
        //             selectedVariation,
        //             trackSeason
        //         );
        //         return true;
        // }

        // return false;
    }

    private static void AssignObjectModData(
        Object obj,
        string modelName,
        AlternativeTextureModel textureModel,
        int variation,
        bool trackSeason = false,
        bool trackSheetId = false
    )
    {
        obj.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = textureModel.Owner;
        obj.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = String.Concat(textureModel.Owner, ".", modelName);

        if (trackSeason)
        {
            obj.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON] = Game1
                .GetSeasonForLocation(Game1.currentLocation)
                .ToString();
        }

        if (trackSheetId)
        {
            obj.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SHEET_ID] = obj.ParentSheetIndex.ToString();
        }

        obj.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = variation.ToString();
    }

    private static void AssignTerrainFeatureModData(
        TerrainFeature terrain,
        string modelName,
        AlternativeTextureModel textureModel,
        int variation,
        bool trackSeason = false
    )
    {
        terrain.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = textureModel.Owner;
        terrain.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = String.Concat(textureModel.Owner, ".", modelName);

        if (trackSeason)
        {
            terrain.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON] = Game1
                .GetSeasonForLocation(terrain.Location)
                .ToString();
        }

        terrain.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = variation.ToString();
    }

    private static void AssignCharacterModData(
        Character character,
        string modelName,
        AlternativeTextureModel textureModel,
        int variation,
        bool trackSeason = false
    )
    {
        character.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = textureModel.Owner;
        character.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = String.Concat(textureModel.Owner, ".", modelName);

        if (trackSeason)
        {
            character.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON] = Game1
                .GetSeasonForLocation(character.currentLocation)
                .ToString();
        }

        character.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = variation.ToString();
    }

    private static void AssignBuildingModData(
        Building building,
        string modelName,
        AlternativeTextureModel textureModel,
        int variation,
        bool trackSeason = false
    )
    {
        building.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = textureModel.Owner;
        building.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = String.Concat(textureModel.Owner, ".", modelName);

        if (trackSeason)
        {
            building.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON] = Game1
                .GetSeasonForLocation(Game1.currentLocation)
                .ToString();
        }

        building.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = variation.ToString();
    }

    private static void AssignDecoratableLocationModData(
        DecoratableLocation decoratableLocation,
        string modelName,
        AlternativeTextureModel textureModel,
        int variation,
        bool trackSeason = false
    )
    {
        decoratableLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = textureModel.Owner;
        decoratableLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = String.Concat(
            textureModel.Owner,
            ".",
            modelName
        );

        if (trackSeason)
        {
            decoratableLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON] = Game1
                .GetSeasonForLocation(Game1.currentLocation)
                .ToString();
        }

        decoratableLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = variation.ToString();
    }

    private static void AssignGameLocationModData(
        GameLocation gameLocation,
        string modelName,
        AlternativeTextureModel textureModel,
        int variation,
        bool trackSeason = false
    )
    {
        gameLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = textureModel.Owner;
        gameLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = String.Concat(textureModel.Owner, ".", modelName);

        if (trackSeason)
        {
            gameLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON] = Game1
                .GetSeasonForLocation(Game1.currentLocation)
                .ToString();
        }

        gameLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = variation.ToString();
    }
}
