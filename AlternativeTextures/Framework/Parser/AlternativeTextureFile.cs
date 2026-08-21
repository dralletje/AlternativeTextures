using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewValley;

namespace AlternativeTextures.Framework.Parser;

record VariationFromFile
{
    public required int Id { get; set; }
    public string? Name { get; set; }
    public List<string> Keywords { get; set; } = [];
    public List<AnimationModel> Animation { get; set; } = [];
    public List<int[]> Tints { get; set; } = [];

    [JsonExtensionData]
    public IDictionary<string, JToken>? ExtraData;
}

record AnimationModel
{
    public required int Frame { get; set; }
    public int Duration { get; set; } = 1000;
    public FrameType Type { get; set; } = FrameType.Default;
}

record AlternativeTextureFile
{
    public required TextureType Type;
    public required int TextureWidth;
    public required int TextureHeight;

    public int Variations = 1;
    public string? ItemName;
    public string? ItemId;
    public List<string> CollectiveNames = [];
    public List<string> CollectiveIds = [];

    public List<string> Keywords = [];
    public List<Season> Seasons = []; // For use by mod user to determine which seasons the texture is valid for

    // public int? DefaultVariation;
    public List<VariationFromFile> ManualVariations = [];

    // public List<AnimationModel> Animation = [];
    public bool IgnoreBuildingColorMask; // Only usable by Type == "Building"

    [Obsolete("No longer used due SMAPI 3.14.0 allowing for passive invalidation checks.")]
    public bool EnableContentPatcherCheck;

    [JsonExtensionData]
    public IDictionary<string, JToken>? AdditionalData { get; set; }
}
