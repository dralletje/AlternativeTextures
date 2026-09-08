using System;
using AlternativeTextures.Framework;
using Dral.Sprites;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AlternativeTextures.Drawing;

/// Alternative version of WorldObject, but this one just contains the data to describe it.
/// It has no position or existance in the world just yet.
/// For trees it contains it's type, growth stage, mossyness, etc,
/// For a building is contains the type and whether it's door is open.
/// Because of this, this will be more classes (TerrainFeature -> [Tree, Grass, etc...])
///
/// This is basically every subclass of the WorldObjects, with only their `XmlElement` / Net values,
/// because those are the actual important ones.
///
/// Eventually I'd want to make a PlainWorldObject class that is all the info a WorldObject has,
/// but for now I'm going to only add things that affect drawing, to keep it simple
///
/// TODO Should I add the AlternativeTexture texture as well? Or do I want to keep that separate?
///
/// TODO What is the use of having inheritance here?
public closed partial record DrawableWorldObject
{
    abstract public ISprite? Sprite(AlternativeTextureModel? model, GameLocation? location);

    // 3. Define the union variants as inner records.
    // public record Object(string Type) : DrawableWorldObject;
    // public record Chest(string Type) : DrawableWorldObject;
    // public record BigCraftable(string Type) : DrawableWorldObject;
    // public record Fence(string Type, int Health, int MaxHealth) : Object(Type);
    // public record Gate(string Type, int Health, int MaxHealth, int Position) : Object(Type);

    // public record TerrainFeature() : DrawableWorldObject;
    // public record Flooring() : TerrainFeature;
    // public record FruitTree() : TerrainFeature;
    // public record Grass() : TerrainFeature;
    // public record HoeDirt() : TerrainFeature;
    // public record LargeTerrainFeature() : TerrainFeature;
    // public record Bush(Bush.BushType Type) : TerrainFeature
    // {
    //     public enum BushType
    //     {
    //         Small,
    //         Medium,
    //         Town,
    //         Tea,
    //         Walnut,
    //     }
    // }
    // public record ResourceClump() : TerrainFeature;

    /// TODO Color??
    // public record Building(string Type) : DrawableWorldObject;
    // public record JunimoHut(bool NoHarvest) : Building("Junimo Hut");
    // public record FishPond() : Building("FishPond");
    // public record PetBowl(bool Watered) : Building("PetBowl");
    // public record Mailbox() : Building("Mailbox");

    // public record Floor() : DrawableWorldObject;

    // public record Wallpaper() : DrawableWorldObject;
}
