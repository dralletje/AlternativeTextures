using System;
using System.Collections.Generic;
using System.Linq;
using AlternativeTextures.Framework;
using AlternativeTextures.PatchDrawMod.Patches.Entities;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using Object = StardewValley.Object;

namespace AlternativeTextures.PatchDrawMod.Patches;

internal class PatchTemplate()
{
    // internal static string GetObjectName(Object obj)
    // {
    //     if (obj.bigCraftable.Value)
    //     {
    //         return !Game1.bigCraftableData.ContainsKey(obj.ItemId) ? obj.name : Game1.bigCraftableData[obj.ItemId].Name;
    //     }
    //     else if (obj is Furniture)
    //     {
    //         var dataSheet = Game1.content.Load<Dictionary<string, string>>("Data\\Furniture");
    //         return !dataSheet.ContainsKey(obj.ItemId) ? obj.name : dataSheet[obj.ItemId].Split('/')[0];
    //     }
    //     else
    //     {
    //         return !Game1.objectData.ContainsKey(obj.ItemId) ? obj.name : Game1.objectData[obj.ItemId].Name;
    //     }
    // }

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

    // internal static string GetFlooringName(Flooring floor)
    // {
    //     return ItemRegistry.GetData(floor.GetData()?.ItemId)?.InternalName ?? string.Empty;
    // }

    // internal static string GetTreeTypeString(Tree tree)
    // {
    //     return tree.treeType.Value switch
    //     {
    //         Tree.bushyTree => "Oak",
    //         Tree.leafyTree => "Maple",
    //         Tree.pineTree => "Pine",
    //         Tree.mahoganyTree => "Mahogany",
    //         Tree.mushroomTree => "Mushroom",
    //         Tree.palmTree => "Palm_1",
    //         Tree.palmTree2 => "Palm_2",
    //         _ => tree.treeType.Value,
    //     };
    // }

    // internal static string GetBushTypeString(Bush bush)
    // {
    //     return bush.size.Value switch
    //     {
    //         0 => "Small",
    //         1 => bush.townBush.Value ? "Town" : "Medium",
    //         2 => "Large",
    //         3 => "Tea",
    //         4 => "Walnut",
    //         _ => String.Empty,
    //     };
    // }

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

    public static AlternativeTextureModel? GetTextureForUse(string? nameMaybe, string? variation)
    {
        if (nameMaybe is not { } name)
            return null;
        if (variation is not { } rawVariationIndex)
            return null;

        var textureIdentifier = UniqueTextureIdentifier.FromString(name, rawVariationIndex);

        if (textureIdentifier.IsDefault)
            return null;

        return AlternativeTextures.textureManager.GetTexture(textureIdentifier);
    }

    public static AlternativeTextureModel? GetTextureForUse(IPaintable? maybePaintable)
    {
        if (maybePaintable is not { } paintable)
            return null;

        if (paintable.TextureIdentifier is null || paintable.TextureIdentifier.IsDefault)
            return null;

        var season = Game1.currentLocation.GetSeason();
        return AlternativeTextures.textureManager.GetTexture(paintable.TextureIdentifier.WithSeason(season));
    }
}
