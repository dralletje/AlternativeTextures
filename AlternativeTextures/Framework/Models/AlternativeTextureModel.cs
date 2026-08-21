using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Incubator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;

namespace AlternativeTextures.Framework.Models;

public record UniqueTextureIdentifier()
{
    public required string Owner { get; init; }
    public required ModelIdentifier ForModel { get; init; }
    public required int Variation { get; init; }
    public required Season Season { get; init; }

    [SetsRequiredMembers]
    public UniqueTextureIdentifier(string owner, ModelIdentifier forModel, int variation, Season season)
        : this()
    {
        Owner = owner;
        ForModel = forModel;
        Variation = variation;
        Season = season;
    }
}

static class UniqueTextureIdentifierExtensions
{
    extension(UniqueTextureIdentifier identifier)
    {
        public static (string owner, TextureType type, string objectName, Season? season) ParseOldId(string name)
        {
            var nameWithoutSeason = name;
            var season = (Season?)null;
            foreach (var s in Enum.GetValues<Season>())
            {
                var seasonSuffix = $"_{s}";
                if (name.EndsWith(seasonSuffix))
                {
                    season = s;
                    nameWithoutSeason = name[..^seasonSuffix.Length];
                }
            }

            var (owner, typeAndNameAndSeason) = nameWithoutSeason.Split(".") switch
            {
                [.. var _owner, var _rest] => (string.Join(".", _owner), _rest),
                _ => throw new ArgumentException($"Not a valid name '{name}' #2"),
            };

            var (type, objectName) = typeAndNameAndSeason.Split("_") switch
            {
                [var _typeString, .. var _objectName] when Enum.TryParse<TextureType>(_typeString, out var _type) => (
                    _type,
                    string.Join("_", _objectName)
                ),
                _ => throw new ArgumentException($"Not a valid name '{name}'"),
            };

            return (owner, type, objectName, season);
        }

        public static UniqueTextureIdentifier FromString(string name, int variation)
        {
            var (owner, type, objectName, season) = ParseOldId(name);
            return new()
            {
                ForModel = new()
                {
                    Type = type,
                    IsName = true,
                    String = objectName,
                },
                Owner = owner,
                Variation = variation,
                Season = season ?? Game1.currentLocation.GetSeason(),
            };
        }

        public static UniqueTextureIdentifier FromString(string name, string variationString) =>
            FromString(name, int.Parse(variationString));

        public bool IsDefault => identifier.Owner is AlternativeTextures.DEFAULT_OWNER || identifier.Variation is -1;

        public string LegacyId => $"{identifier.Owner}.{identifier.ForModel.Type}_{identifier.ForModel.String}";

        public TextureIdentifierWithoutSeason WithoutSeason =>
            new(identifier.Owner, identifier.ForModel, identifier.Variation);
    }
}

public record TextureIdentifierWithoutSeason(
    [property: JsonProperty(Required = Required.Always)] string Owner,
    [property: JsonProperty(Required = Required.Always)] ModelIdentifier ForModel,
    [property: JsonProperty(Required = Required.Always)] int Variation
) { }

static class TextureIdentifierWithoutSeasonExtensions
{
    extension(TextureIdentifierWithoutSeason identifier)
    {
        public bool IsDefault => identifier.Owner is AlternativeTextures.DEFAULT_OWNER || identifier.Variation is -1;

        public string LegacyId => $"{identifier.Owner}.{identifier.ForModel.Type}_{identifier.ForModel.String}";
        public string LegacyIdWithVariant =>
            $"{identifier.Owner}.{identifier.ForModel.Type}_{identifier.ForModel.String}_{identifier.Variation}";

        public UniqueTextureIdentifier WithSeason(Season season) =>
            new(identifier.Owner, identifier.ForModel, identifier.Variation, season);

        public static TextureIdentifierWithoutSeason FromString(string name, int variation)
        {
            var (owner, type, objectName, season) = UniqueTextureIdentifier.ParseOldId(name);

            /// Error is `season` is not null?
            return new(owner, type.WithName(objectName), variation);
        }

        public static TextureIdentifierWithoutSeason FromString(string name, string variationString) =>
            FromString(name, int.Parse(variationString));

        public static TextureIdentifierWithoutSeason DefaultFor(ModelIdentifier modelIdentifier) =>
            new(AlternativeTextures.DEFAULT_OWNER, modelIdentifier, -1);
    }
}

public record TextureQuery
{
    public required ModelIdentifier ModelIdentifier;
    public required Season Season;
}

public record AlternativeTextureModel
{
    public required ModelIdentifier ForModel;
    public required Season Season;
    public required IManifest PackManifest;
    public required int Variation;

    public required string? DisplayName;
    public required int TextureWidth;
    public required int TextureHeight;
    public required DrawableTexture Texture;

    public bool IgnoreBuildingColorMask; // Only usable by Type == "Building"
    public List<string> Keywords = [];
    public List<AnimationModel> Animation = [];

    public string Owner => PackManifest.UniqueID;
    public UniqueTextureIdentifier UniqueIdentifier => new(Owner, ForModel, Variation, Season);
    public TextureIdentifierWithoutSeason UniqueIdentifierWithoutSeason => new(Owner, ForModel, Variation);
}

static class AlternativeTextureModelExtensions
{
    extension(AlternativeTextureModel textureModel)
    {
        /////////////////////////////

        // internal string ModelName
        // {
        //     get { return Season is null ? $"{Type}_{ItemName}" : $"{Type}_{ItemName}_{Season}"; }
        // }

        /// <summary>
        /// This is what is currently saved to items, so we need it for compatibitily.
        /// Biggest problem is that it does not contain the variant, that is currently stored separately.
        /// This is still used for a bunch of lookups, so as a workaround `.getTexture(int variation)` will
        /// actually look up the actual texture model in the list of all textures.
        /// </summary>
        internal string LegacyId => $"{textureModel.Owner}.{textureModel.ForModel.Type}_{textureModel.ForModel.String}";
        internal string LegacyIdWithVariant =>
            $"{textureModel.Owner}.{textureModel.ForModel.Type}_{textureModel.ForModel.String}_{textureModel.Variation}";

        [Obsolete("Use `.LegacyId`")]
        internal string TextureId => textureModel.LegacyId;

        public static int MAX_TEXTURE_HEIGHT => 16384;

        /// TODO Make this use UniqueIdentifier?
        [Obsolete("Use `.LegacyId`")]
        public string GetId() => textureModel.TextureId;

        public bool IsUsingItemId() => textureModel.ForModel.IsName;

        [Obsolete("Uhhh")]
        public string? GetNameWithSeason()
        {
            return $"{textureModel.ForModel.Type}_{textureModel.ForModel.String}";
        }

        [Obsolete("Variations are separate entities now")]
        public int GetVariations()
        {
            return 1;
            // var manualCount = ManualVariations.Count(v => v.Id >= 0);
            // return manualCount > 0 ? manualCount : Variations;
        }

        [Obsolete("Use version without variation")]
        public List<AnimationModel> GetAnimationData(int variation) => [];

        public List<AnimationModel> GetAnimationData() => [];

        [Obsolete("Use version without variation")]
        public AnimationModel? GetAnimationDataAtIndex(int variation, int index) => null;

        public AnimationModel? GetAnimationDataAtIndex(int index) => null;

        // public AnimationModel? GetAnimationDataAtIndex(int variation, int index)
        // {
        //     var animationData = GetAnimationData(variation);
        //     if (animationData is null || animationData.Count == 0)
        //     {
        //         return null;
        //     }
        //     else if (animationData.Count <= index)
        //     {
        //         index = 0;
        //     }

        //     return animationData.ElementAt(index);
        // }

        // public int GetNextValidFrameFromIndex(int variation, int index, bool isMachineActive)
        // {
        //     var animationData = GetAnimationData(variation);

        //     index += 1;
        //     if (index >= GetAnimationData(variation).Count)
        //     {
        //         index = 0;
        //         return index;
        //     }

        //     return IsFrameValid(variation, index, isMachineActive)
        //         ? index
        //         : GetNextValidFrameFromIndex(variation, index, isMachineActive);
        // }

        [Obsolete("Should get `.Texture` directly")]
        public Texture2D GetTexture(int variation)
        {
            var identifier = textureModel.UniqueIdentifier with { Variation = variation };
            var texture =
                AlternativeTextures.textureManager.GetTexture(identifier)
                ?? throw new ArgumentException($"Can't found texture for {identifier}");

            if (texture.Texture.Texture.IsDisposed)
            {
                Monitor.LogOnce(
                    $"Error drawing the texture {textureModel.TextureId}: It was incorrectly disposed!",
                    StardewModdingAPI.LogLevel.Warn
                );
                Monitor.LogOnce(textureModel.ToString(), LogLevel.Trace);
                return AlternativeTextures.textureManager.ErrorTexture;
            }

            return texture.Texture.Texture;
        }

        // public Color GetRandomTint(int variation)
        // {
        //     if (!HasTint(variation))
        //     {
        //         return Color.White;
        //     }

        //     var tints = ManualVariations.First(v => v.Id == variation).Tints;
        //     var selectedTint = tints[Game1.random.Next(tints.Count)];
        //     return new Color(selectedTint[0], selectedTint[1], selectedTint[2], selectedTint[3]);
        // }

        // public bool HasKeyword(string variationString, string keyword)
        // {
        //     return int.TryParse(variationString, out var variation) && HasKeyword(variation, keyword);
        // }

        // public bool HasKeyword(int variation, string keyword)
        // {
        //     return ManualVariations.Any(v => v.Id == variation)
        //         ? ManualVariations
        //             .First(v => v.Id == variation)
        //             .Keywords.Any(k => k.Contains(keyword, StringComparison.OrdinalIgnoreCase))
        //         : Keywords.Any(k => k.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        // }

        // public bool HasAnimation(int variation)
        // {
        //     return Animation.Count > 0 || ManualVariations.Any(v => v.Id == variation && v.HasAnimation());
        // }

        [Obsolete("Variations are separate textures now")]
        public int Variations => 1;

        [Obsolete("Smakes Smo Smense.")]
        public Dictionary<int, Texture2D> Textures => [];

        [Obsolete("Smakes Smo Smense!!!!")]
        public int GetTextureOffset(int _) => 0;

        [Obsolete("Just compare the `.ForModel.Type`")]
        public string? ItemName => textureModel.ForModel.IsName ? textureModel.ForModel.String : null;

        [Obsolete("Use version without variation")]
        public int GetNextValidFrameFromIndex(int variation, int index, bool isMachineActive) => 0;

        [Obsolete("Use version without variation")]
        public Color GetRandomTint(int? variation) => Color.White;

        [Obsolete("Use version without variation")]
        public bool HasAnimation(int variation) => false;

        [Obsolete("Use version without variation")]
        public bool HasTint(int variation) => false;

        [Obsolete("Use version without variation")]
        internal bool IsFrameValid(int variation, int currentFrame, bool isMachineActive) => false;

        [Obsolete("Use `.ForModel.Type")]
        internal string GetTextureType() => textureModel.ForModel.Type.ToString();

        ///////////////////////////////////////////////////

        public int GetNextValidFrameFromIndex(int index, bool isMachineActive) => 0;

        public Color GetRandomTint() => Color.White;

        public bool HasAnimation() => false;

        public bool HasTint() => false;

        public string GetTokenId() =>
            $"{textureModel.Owner}/{textureModel.ForModel.Type}/{textureModel.ForModel.String}/{textureModel.Variation}";

        internal bool IsFrameValid(int currentFrame, bool isMachineActive) => false;

        // public string GetTokenId(int? variation = null)
        // {
        //     var seasonSuffix = String.IsNullOrEmpty(Season) ? String.Empty : String.Concat("_", Season);
        //     var variationSuffix = variation is null ? String.Empty : String.Concat("_", variation);
        //     return String.Concat(Owner, ".", ItemName, seasonSuffix, variationSuffix);
        // }

        // public bool HasTint(int variation)
        // {
        //     return ManualVariations.Any(v => v.Id == variation && v.HasTint());
        // }

        // internal bool IsFrameValid(int variation, int currentFrame, bool isMachineActive)
        // {
        //     var animationData = GetAnimationDataAtIndex(variation, currentFrame);
        //     return animationData is not null
        //         && (animationData.Type is not FrameType.MachineActive || isMachineActive is true)
        //         && (animationData.Type is not FrameType.MachineIdle || isMachineActive is false);
        // }

        // public override string ToString()
        // {
        //     return $"\n[\n"
        //         + $"\tOwner: {Owner} | ItemName: {ItemName} | ItemId: {ItemId} | Type: {Type} | Season: {Season}\n"
        //         + $"\tTextureWidth x TextureHeight: [{TextureWidth}x{TextureHeight}] | Variations: {Variations}\n";
        // }
    }
}


// public class AlternativeTextureModel
// {
//     public string? Owner;
//     public string? PackName;
//     public string? Author;
//     public string? ItemName
//     {
//         get { return string.IsNullOrEmpty(field) ? ItemId : field; }
//         set;
//     }

//     public string? ItemId;
//     public List<string> CollectiveNames = [];
//     public List<string> CollectiveIds = [];

//     public required TextureType Type;
//     public required int TextureWidth;
//     public required int TextureHeight;
//     public required int Variations = 1;

//     [Obsolete("No longer used due SMAPI 3.14.0 allowing for passive invalidation checks.")]
//     public bool EnableContentPatcherCheck;

//     public bool IgnoreBuildingColorMask; // Only usable by Type == "Building"
//     public List<string> Keywords = [];
//     public List<string> Seasons = []; // For use by mod user to determine which seasons the texture is valid for
//     public int? DefaultVariation;
//     public List<AnimationModel> Animation = [];

//     internal string? Season; // Used by framework to split the Seasons property into individual AlternativeTextureModel models
//     internal string? TileSheetPath;
//     internal Dictionary<int, Texture2D> Textures = [];

//     internal string ModelName
//     {
//         get { return Season is null ? $"{Type}_{ItemName}" : $"{Type}_{ItemName}_{Season}"; }
//     }
//     internal string TextureId
//     {
//         get { return $"{Owner}.{ModelName}"; }
//     }

//     public static int MAX_TEXTURE_HEIGHT
//     {
//         get { return 16384; }
//     }

//     public AlternativeTextureModel ShallowCopy()
//     {
//         return (AlternativeTextureModel)this.MemberwiseClone();
//     }

//     public string GetTextureType()
//     {
//         return Type.ToString();
//     }

//     public string GetId()
//     {
//         return TextureId;
//     }

//     public bool IsUsingItemId()
//     {
//         return string.IsNullOrEmpty(ItemId) is false;
//     }

//     public string GetTokenId(int? variation = null)
//     {
//         var seasonSuffix = String.IsNullOrEmpty(Season) ? String.Empty : String.Concat("_", Season);
//         var variationSuffix = variation is null ? String.Empty : String.Concat("_", variation);
//         return String.Concat(Owner, ".", ItemName, seasonSuffix, variationSuffix);
//     }

//     public string? GetNameWithSeason()
//     {
//         return ModelName;
//     }

//     public int GetVariations()
//     {
//         var manualCount = ManualVariations.Count(v => v.Id >= 0);
//         return manualCount > 0 ? manualCount : Variations;
//     }

//     public bool IsManualVariationsValid()
//     {
//         return ManualVariations.Any(v => v.Id == 1) is false || ManualVariations.Any(v => v.Id == 0) is true;
//     }

//     public List<AnimationModel> GetAnimationData(int variation)
//     {
//         var manualVariation = ManualVariations.FirstOrDefault(v => v.Id == variation && v.HasAnimation());
//         return manualVariation != null ? manualVariation.Animation : Animation;
//     }

//     public AnimationModel? GetAnimationDataAtIndex(int variation, int index)
//     {
//         var animationData = GetAnimationData(variation);
//         if (animationData is null || animationData.Count == 0)
//         {
//             return null;
//         }
//         else if (animationData.Count <= index)
//         {
//             index = 0;
//         }

//         return animationData.ElementAt(index);
//     }

//     public int GetNextValidFrameFromIndex(int variation, int index, bool isMachineActive)
//     {
//         var animationData = GetAnimationData(variation);

//         index += 1;
//         if (index >= GetAnimationData(variation).Count)
//         {
//             index = 0;
//             return index;
//         }

//         return IsFrameValid(variation, index, isMachineActive)
//             ? index
//             : GetNextValidFrameFromIndex(variation, index, isMachineActive);
//     }

//     public Color GetRandomTint(int variation)
//     {
//         if (!HasTint(variation))
//         {
//             return Color.White;
//         }

//         var tints = ManualVariations.First(v => v.Id == variation).Tints;
//         var selectedTint = tints[Game1.random.Next(tints.Count)];
//         return new Color(selectedTint[0], selectedTint[1], selectedTint[2], selectedTint[3]);
//     }

//     public bool HasKeyword(string variationString, string keyword)
//     {
//         return int.TryParse(variationString, out var variation) && HasKeyword(variation, keyword);
//     }

//     public bool HasKeyword(int variation, string keyword)
//     {
//         return ManualVariations.Any(v => v.Id == variation)
//             ? ManualVariations
//                 .First(v => v.Id == variation)
//                 .Keywords.Any(k => k.Contains(keyword, StringComparison.OrdinalIgnoreCase))
//             : Keywords.Any(k => k.Contains(keyword, StringComparison.OrdinalIgnoreCase));
//     }

//     public bool HasAnimation(int variation)
//     {
//         return Animation.Count > 0 || ManualVariations.Any(v => v.Id == variation && v.HasAnimation());
//     }

//     public bool HasTint(int variation)
//     {
//         return ManualVariations.Any(v => v.Id == variation && v.HasTint());
//     }

//     internal bool IsFrameValid(int variation, int currentFrame, bool isMachineActive)
//     {
//         var animationData = GetAnimationDataAtIndex(variation, currentFrame);
//         return animationData is not null
//             && (animationData.Type is not FrameType.MachineActive || isMachineActive is true)
//             && (animationData.Type is not FrameType.MachineIdle || isMachineActive is false);
//     }
// }
