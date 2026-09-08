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
public closed record DrawableWorldObject
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
    /// TODO How should I enforce that this is a tree model identifier?
    /// .... I feel like I should revamp ModelIdentifier to be a closed class with subclass per type
    public record Tree(ModelIdentifier Model) : DrawableWorldObject
    {
        public int GrowthStage { get; init; } = 5;
        public float Health { get; init; } = 10f;
        public bool Flipped { get; init; } = false;
        // public bool Tapped { get; init; } = false;
        // public bool Falling { get; init; } = false;
        public bool HasSeed { get; init; } = false;
        public bool HasMoss { get; init; } = false;
        public bool WasShakenToday { get; init; } = false;
        public bool Fertilized { get; init; } = false;

        public float ShakeRotation { get; init; } = 0;

        public Tree With(StardewValley.TerrainFeatures.Tree? tree)
        {
            if (tree is null)
            {
                return this;
            }
            else
            {
                if (ModelIdentifier.From(tree) != Model)
                    throw new ArgumentException(".With mismatch tree derived model and provided model");
                return this with
                {
                    GrowthStage = tree.growthStage.Value,
                    Health = tree.health.Value,
                    Flipped = tree.flipped.Value,
                    HasSeed = tree.hasSeed.Value,
                    HasMoss = tree.hasMoss.Value,
                    WasShakenToday = tree.wasShakenToday.Value,
                    Fertilized = tree.fertilized.Value,
                    ShakeRotation = tree.shakeRotation,
                };
            }
        }

        public static bool IsLeafy(StardewValley.GameData.WildTrees.WildTreeData data, GameLocation? location)
        {
            if (location is null) return data.IsLeafy;
            return data switch
            {
                { IsLeafy: false } => false,
                { IsLeafyInWinter: true } when location.IsWinterHere() => true,
                { IsLeafyInFall: true } when location.IsFallHere() => true,
                { IsLeafy: true } => true,
            };
        }


        string StardewType => Model.Name switch
        {
            "Oak" => StardewValley.TerrainFeatures.Tree.bushyTree,
            "Maple" => StardewValley.TerrainFeatures.Tree.leafyTree,
            "Pine" => StardewValley.TerrainFeatures.Tree.pineTree,
            "Mahogany" => StardewValley.TerrainFeatures.Tree.mahoganyTree,
            "Mushroom" => StardewValley.TerrainFeatures.Tree.mushroomTree,
            "Palm_1" => StardewValley.TerrainFeatures.Tree.palmTree,
            "Palm_2" => StardewValley.TerrainFeatures.Tree.palmTree2,
            _ => Model.Name,
        };

        string? DefaultTextureAt(StardewValley.GameData.WildTrees.WildTreeData data, GameLocation? location)
        {
            if (data is null || data.Textures is null || data.Textures.Count == 0)
            {
                return null;
            }

            if (location is null)
            {
                return data.Textures[0].Texture;
            }
            else
            {
                foreach (var texture in data.Textures)
                {
                    if (location != null && location.IsGreenhouse && texture.Season.HasValue)
                    {
                        if (texture.Season == Season.Spring)
                        {
                            return texture.Texture;
                        }
                    }
                    else if ((!texture.Season.HasValue || texture.Season == location.GetSeason()) && (texture.Condition == null || GameStateQuery.CheckConditions(texture.Condition, location)))
                    {
                        return texture.Texture;
                    }
                }
                return data.Textures[0].Texture;
            }
        }


        override public ISprite? Sprite(AlternativeTextureModel? model, GameLocation? location)
        {
            var tree_data = StardewValley.TerrainFeatures.Tree.GetWildTreeDataDictionary();
            if (!tree_data.TryGetValue(StardewType, out var data)) return null;
            if (data.Textures.Count is 0) return null;

            /// Not using Location::Season or GameStateQuery yet, which Tree::draw does
            /// TODO Make Sprite accept location?
            var texture = model?.Texture ?? (DefaultTextureAt(data, location) is { } textureName ? Game1.content.Load<Texture2D>(textureName) : null);
            if (texture is null) return null;

            if (GrowthStage < 5)
            {
                var fertilizedColor = Fertilized ? Color.HotPink : Color.White;
                var source_rect = GrowthStage switch
                {
                    0 => new Rectangle(32, 128, 16, 16),
                    1 => new Rectangle(0, 128, 16, 16),
                    2 => new Rectangle(16, 128, 16, 16),
                    _ => new Rectangle(0, 96, 16, 32),
                };
                // var destination_y = GrowthStage switch
                // {
                //     0 => new Rectangle(32, 128, 16, 16),
                //     1 => new Rectangle(0, 128, 16, 16),
                //     2 => new Rectangle(16, 128, 16, 16),
                //     _ => new Rectangle(0, 96, 16, 32),
                // };
                return texture
                    .Clip(source_rect)
                    .Rotate(ShakeRotation, new Vector2(8f, (GrowthStage >= 3) ? 32 : 16))
                    // .Flip(Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None)
                    .MultiplyColor(fertilizedColor);

                // spriteBatch.Draw(
                //     texture.Value,
                //     new Vector2(
                //         32f,
                //         64f - (float)(value.Height * 4 - 64) + (float)((growthStage.Value >= 3) ? 128 : 64)
                //     ),
                //     value,
                //     fertilizedColor,
                //     shakeRotation,
                //     new Vector2(8f, (growthStage.Value >= 3) ? 32 : 16),
                //     4f,
                //     flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                //     (growthStage.Value == 0) ? 0.0001f : (num / 10000f)
                // );
            }
            else
            {
                var originalTreeTopSourceRect = StardewValley.TerrainFeatures.Tree.treeTopSourceRect;
                var originalStumpSourceRect = StardewValley.TerrainFeatures.Tree.stumpSourceRect;
                var destinationWidth = originalTreeTopSourceRect.Width;
                var hasMossTexture = texture.Width >= destinationWidth * 2;
                var hasAlternativeSprite = texture.Width >= destinationWidth * 3;
                var wantsAlternativeSprite = (data.UseAlternateSpriteWhenSeedReady && HasSeed) || (data.UseAlternateSpriteWhenNotShaken && !WasShakenToday);
                var stumpSourceRect = originalStumpSourceRect with
                {
                    X =
                        hasMossTexture && HasMoss
                        ? originalStumpSourceRect.X + (destinationWidth * 2)
                        : originalStumpSourceRect.X
                };
                var treeTopSourceRect = originalTreeTopSourceRect with
                {
                    X =
                        (hasAlternativeSprite && wantsAlternativeSprite)
                        ? originalTreeTopSourceRect.X + (destinationWidth * 1)
                        : hasMossTexture && HasMoss
                        ? originalTreeTopSourceRect.X + (destinationWidth * 2)
                        : originalTreeTopSourceRect.X
                };

                var leafyShadow = Game1.mouseCursors.Clip(StardewValley.TerrainFeatures.Tree.shadowSourceRect);
                var nonLeafyShadow = Game1.mouseCursors_1_6.Clip(new Rectangle(469, 298, 42, 31));
                /// new Vector2(tile.X * 64f + 32f, tile.Y * 64f + 64f))
                /// new Vector2(tile.X * 64f - 51f, tile.Y * 64f - 16f)
                if (IsLeafy(data, location: null))
                {
                    // spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(tile.X * 64f - 51f, tile.Y * 64f - 16f)), shadowSourceRect, Color.White * (MathF.PI / 2f - Math.Abs(shakeRotation)), 0f, Vector2.Zero, 4f, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 1E-06f);
                }
                // new Vector2(tile.X * 64f - 51f, tile.Y * 64f - 16f)
                else
                {
                    // spriteBatch.Draw(Game1.mouseCursors_1_6, Game1.GlobalToLocal(Game1.viewport, ), new Microsoft.Xna.Framework., Color.White * (MathF.PI / 2f - Math.Abs(shakeRotation)), 0f, Vector2.Zero, 4f, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 1E-06f);
                }



                /// TODO Health and falling stuff
                return new ClippableSprite([
                    (IsLeafy(data, location: null) ? leafyShadow : nonLeafyShadow).Translate(new(48 + 6 + -51, 96 - 4 + -16)),
                    texture.Clip(stumpSourceRect).Translate(new(16, 96 + -32)),
                    texture.Clip(treeTopSourceRect),
                ]);
            }
        }
    }

    /// TODO Color??
    // public record Building(string Type) : DrawableWorldObject;
    // public record JunimoHut(bool NoHarvest) : Building("Junimo Hut");
    // public record FishPond() : Building("FishPond");
    // public record PetBowl(bool Watered) : Building("PetBowl");
    // public record Mailbox() : Building("Mailbox");

    // public record Floor() : DrawableWorldObject;

    // public record Wallpaper() : DrawableWorldObject;
}
