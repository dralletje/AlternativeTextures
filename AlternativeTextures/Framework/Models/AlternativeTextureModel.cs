using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using AlternativeTextures.Framework.Enums;
using ConsoleLog;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace AlternativeTextures.Framework.Models;

public readonly record struct UniqueTextureIdentifier(
    string Owner,
    ModelIdentifier ForModel,
    int Variation,
    Season Season
)
{
    public static UniqueTextureIdentifier FromString(string name, int variation)
    {
        var (ownerAndType, objectName, season) = name.Split("_", 3) switch
        {
            [var _ownerAndType, var _objectName] => (_ownerAndType, _objectName, Game1.currentLocation.GetSeason()),
            [var _ownerAndType, var _objectName, var _seasonString]
                when Enum.TryParse<Season>(_seasonString, out var _season) => (_ownerAndType, _objectName, _season),
            _ => throw new ArgumentException($"Not a valid name '{name}'"),
        };
        var (owner, type) = ownerAndType.Split(".") switch
        {
            [.. var _owner, var _typeString] when Enum.TryParse<TextureType>(_typeString, out var _type) => (
                string.Join(".", _owner),
                _type
            ),
            _ => throw new ArgumentException($"Not a valid name '{name}' #2"),
        };

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
        };
    }

    public static UniqueTextureIdentifier FromString(string name, string variationString)
    {
        return FromString(name, int.Parse(variationString));
    }
}

/// Make this a distriminated as soon as you can
public record ModelIdentifier
{
    public required TextureType Type;
    public required bool IsName;
    public required string String;

    public override string ToString()
    {
        return $"{Type}_{String}";
    }

    public string Name => String;

    /// TODO Make this work for IDs too?
    public static ModelIdentifier? FromString(string modelIdentifierString)
    {
        return modelIdentifierString.Split("_", 2) switch
        {
            [var typeString, var name] => EnumUtil.ParseOrNull<TextureType>(typeString) switch
            {
                { } modelType => new ModelIdentifier()
                {
                    Type = modelType,
                    IsName = true,
                    String = name,
                },
                null => null,
            },
            _ => null,
        };
    }
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
    public int? DefaultVariation;
    public List<AnimationModel> Animation = [];

    public string Owner => PackManifest.UniqueID;
    public UniqueTextureIdentifier UniqueIdentifier => new(Owner, ForModel, Variation, Season);
    public (string Owner, ModelIdentifier ForModel, int Variation) UniqueIdentifierWithoutSeason =>
        (Owner, ForModel, Variation);

    public TextureIdentifier TextureIdentifier =>
        new()
        {
            Owner = Owner,
            Name = TextureId,
            Variation = Variation,
        };

    /////////////////////////////

    // internal string ModelName
    // {
    //     get { return Season is null ? $"{Type}_{ItemName}" : $"{Type}_{ItemName}_{Season}"; }
    // }

    /// TODO Include variant?
    internal string ModelName => $"{ForModel.Type}_{ForModel.String}";
    internal string TextureId => $"{Owner}.{ForModel.Type}_{ForModel.String}";

    public static int MAX_TEXTURE_HEIGHT = 16384;

    /// TODO Make this use UniqueIdentifier?
    public string GetId() => TextureId;

    public bool IsUsingItemId() => ForModel.IsName;

    public string? GetNameWithSeason()
    {
        return ModelName;
    }

    public int GetVariations()
    {
        return 1;
        // var manualCount = ManualVariations.Count(v => v.Id >= 0);
        // return manualCount > 0 ? manualCount : Variations;
    }

    public List<AnimationModel> GetAnimationData(int variation)
    {
        return [];
        // var manualVariation = ManualVariations.FirstOrDefault(v => v.Id == variation && v.HasAnimation());
        // return manualVariation != null ? manualVariation.Animation : Animation;
    }

    public AnimationModel? GetAnimationDataAtIndex(int variation, int index)
    {
        var animationData = GetAnimationData(variation);
        if (animationData is null || animationData.Count == 0)
        {
            return null;
        }
        else if (animationData.Count <= index)
        {
            index = 0;
        }

        return animationData.ElementAt(index);
    }

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
        var identifier = new UniqueTextureIdentifier()
        {
            ForModel = ForModel,
            Owner = Owner,
            Season = Season,
            Variation = variation,
        };
        var texture =
            AlternativeTextures.textureManager.GetTexture(identifier)
            ?? throw new ArgumentException($"Can't found texture for {identifier}");

        if (texture.Texture.Texture.IsDisposed)
        {
            Monitor.LogOnce(
                $"Error drawing the texture {TextureId}: It was incorrectly disposed!",
                StardewModdingAPI.LogLevel.Warn
            );
            Monitor.LogOnce(this.ToString(), StardewModdingAPI.LogLevel.Trace);
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

    [Obsolete("Variations are separate textures now.")]
    public List<VariationModel> ManualVariations => [];

    [Obsolete("Smakes Smo Smense.")]
    public Dictionary<int, Texture2D> Textures => [];

    [Obsolete("Smakes Smo Smense!!!!")]
    public int GetTextureOffset(int _) => 0;

    public string? ItemName => ForModel.IsName ? ForModel.String : null;

    public int GetNextValidFrameFromIndex(int variation, int index, bool isMachineActive) => 0;

    public Color GetRandomTint(int? variation) => Color.White;

    public bool HasAnimation(int variation) => false;

    public bool HasTint(int variation) => false;

    public string GetTokenId(int? variation = null) => "";

    internal bool IsFrameValid(int variation, int currentFrame, bool isMachineActive) => false;

    [Obsolete("Use `.ForModel.Type")]
    internal string GetTextureType() => ForModel.Type.ToString();

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
//     public List<VariationModel> ManualVariations = [];
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
