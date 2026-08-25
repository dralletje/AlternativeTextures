using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AlternativeTextures.Framework;
using AlternativeTextures.Framework.Managers;
using Newtonsoft.Json;
using SkiaSharp;
using StardewModdingAPI;
using StardewValley;

namespace AlternativeTextures.ContentPackLoaderMod;

static class ContentPackLoaderAsync
{
    public static void Load(IModHelper Helper, TextureManager textureManager)
    {
        var collectiveLoadingStopwatch = Stopwatch.StartNew();

        using var collection = new BlockingCollection<AlternativeTextureModel>();

        /// Load the packs in parallel
        var parallelTask = Task.Run(() =>
        {
            var justLoadingStopwatch = Stopwatch.StartNew();
            var result = Parallel.ForEach(
                Helper.ContentPacks.GetOwned(),
                // new ParallelOptions { MaxDegreeOfParallelism = 1 },
                (contentPack) =>
                {
                    var TAG = $"[{contentPack.Manifest.Name}]".Blue();
                    // Monitor.Log(
                    //     $"Loading textures from pack: {contentPack.Manifest.Name} {contentPack.Manifest.Version} by {contentPack.Manifest.Author}",
                    //     LogLevel.Debug
                    // );
                    var individualLoadingStopwatch = Stopwatch.StartNew();

                    try
                    {
                        var textureModels = LoadPackContents(contentPack.DirectoryPath, contentPack.Manifest).ToList();

                        var shouldBeUnique = textureModels.GroupBy(model =>
                            (model.Owner, model.ForModel, model.Season)
                        );

                        foreach (var matches in shouldBeUnique)
                        {
                            var hashset = new HashSet<int>(matches.Select(x => x.Variation));
                            if (hashset.Count == matches.Count())
                            {
                                foreach (var match in matches)
                                {
                                    collection.Add(match);
                                }
                            }
                            else
                            {
                                // Console.Log($"{matches.Count()} textures found for {matches.Key}, not adding either");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Monitor.Log($"Error loading content pack {contentPack.Manifest.Name}: {ex}", LogLevel.Error);
                    }

                    individualLoadingStopwatch.Stop();
                    // Monitor.Log(
                    //     $"{TAG} finished loading in {individualLoadingStopwatch.Elapsed.TotalSeconds} seconds",
                    //     LogLevel.Trace
                    // );
                }
            );

            justLoadingStopwatch.Stop();
            Monitor.Log($"Ending things: {result} in {justLoadingStopwatch.Elapsed.TotalSeconds}", LogLevel.Trace);
            collection.CompleteAdding();
        });

        /// Do the last check (and the adding of the textures) in the main thread
        /// because I don't trust textureManager to be thread safe.
        foreach (var textureModel in collection.GetConsumingEnumerable())
        {
            // var _ = textureModel.Texture;
            textureManager.AddAlternativeTexture(textureModel);
        }
        collectiveLoadingStopwatch.Stop();

        // Monitor.Log(
        //     $"Finished loading all content packs in {collectiveLoadingStopwatch.Elapsed.TotalSeconds} seconds",
        //     LogLevel.Trace
        // );
        Console.Log($"Finished loading all content packs in {collectiveLoadingStopwatch.Elapsed.TotalSeconds} seconds");

        // Clear the wallpaper / flooring cache
        Helper.GameContent.InvalidateCache("Data/AdditionalWallpaperFlooring");
    }

    record TextureGroup
    {
        public required (ModelIdentifier, Season) Id { get; init; }
        public required List<AlternativeTextureModel> Models { get; init; }
        public required bool IsImplicitSeason { get; init; }
        public required string TextureJsonPath { get; init; }
    }

    internal static IEnumerable<AlternativeTextureModel> LoadPackContents(
        // IContentPack contentPack,
        string contentPackRootFolder,
        IManifest contentPackManifest
    )
    {
        // var contentPackRootFolder = contentPack.DirectoryPath;
        // var contentPackName = contentPack.Manifest.Name;

        var texturesRootFolder = Path.Combine(contentPackRootFolder, "Textures");
        var textureFolders = new DirectoryInfo(texturesRootFolder).GetDirectories("*", SearchOption.AllDirectories);

        if (textureFolders.Length == 0)
            throw new ContentPackException("No folders found inside content pack");

        // var mutable_TextureModels = new List<AlternativeTextureModel>();
        var mutable_files = new List<TextureGroup>();
        var TAG = $"[{contentPackManifest.Name}]".Blue();

        // Load in the alternative textures
        foreach (var textureFolder in textureFolders)
        {
            var relativeFolderName = textureFolder.FullName.Replace(contentPackRootFolder, string.Empty)[1..];
            try
            {
                if (File.Exists(Path.Combine(textureFolder.FullName, "texture.json")) is false)
                {
                    if (textureFolder.GetDirectories().Length == 0)
                        throw new ContentPackException("Texture folder is missing texture.json");

                    continue;
                }

                var modelPath = Path.Combine(textureFolder.FullName, "texture.json");
                var text = File.ReadAllText(modelPath);
                var file =
                    JsonConvert.DeserializeObject<AlternativeTextureFile>(text, SaneJson.Settings)
                    ?? throw new ContentPackTextureException("Couldn't parse texture.json");

                Console.Log($"Extra: {file.Extra}");

                var ids = file.ItemId is null ? file.CollectiveIds : [.. file.CollectiveIds, file.ItemId];
                var names = file.ItemName is null ? file.CollectiveNames : [.. file.CollectiveNames, file.ItemName];

                if (ids.Count is 0 && names.Count is 0)
                    throw new ContentPackException("Texture has no ids or names");

                if (
                    file.ManualVariations.Any(v => v.Id == 0) is false
                    && file.ManualVariations.Any(v => v.Id == 1) is true
                )
                    throw new ContentPackTextureException("ManualVariations contains an Id = 1, but not an Id = 0");

                /// Name changes for backward compatibility
                names = names
                    .Select(name =>
                        file switch
                        {
                            /// Backwards compatibility
                            { Type: TextureType.Building }
                                when name.Equals("Log Cabin", StringComparison.OrdinalIgnoreCase) => "Cabin",
                            { Type: TextureType.Building }
                                when name.Equals("Plank Cabin", StringComparison.OrdinalIgnoreCase) => "Cabin",
                            { Type: TextureType.Building }
                                when name.Equals("Stone Cabin", StringComparison.OrdinalIgnoreCase) => "Cabin",

                            /// Something with forcing the item name to "Grass" for translations?
                            /// Don't really know
                            { Type: TextureType.Grass } => "Grass",

                            _ => name,
                        }
                    )
                    .ToList();

                var type = file.Type switch
                {
                    /// Backwards compatible renaming
                    TextureType.Craftable
                        when names.Any(n => n.Equals("Artifact Spot", StringComparison.OrdinalIgnoreCase)) =>
                        TextureType.ArtifactSpot,

                    _ => file.Type,
                };

                List<ModelIdentifier> models =
                [
                    .. ids.Select(id => new ModelIdentifier
                    {
                        Type = type,
                        String = id,
                        IsName = false,
                    }),
                    .. names.Select(name => new ModelIdentifier
                    {
                        Type = type,
                        String = name,
                        IsName = true,
                    }),
                ];

                var variations =
                    file.ManualVariations.Count == 0
                        ? Enumerable.Range(0, file.Variations).Select(index => new VariationFromFile() { Id = index })
                        : file.ManualVariations;

                var textureVariations = File.Exists(Path.Combine(textureFolder.FullName, "texture.png"))
                    ? LoadTexturesFromSingleFile(file, textureFolder.FullName)
                    : LoadTexturesFromMultipleFiles(file, textureFolder.FullName);

                /// Not checking now if there are more `textureVariations` than there are `variations`,
                /// but that might something we might want to add
                var variationCombination = Enumerable.Zip(variations, textureVariations);

                /// Add `null` as a Season when no season are defined
                /// (Could also add all the seasons, but that feels a bit extreme)
                var seasons =
                    file.Seasons.Count == 0 ? [Season.Spring, Season.Summer, Season.Fall, Season.Winter] : file.Seasons;

                foreach (var modelIdentifier in models)
                {
                    foreach (var season in seasons)
                    {
                        var mutable_TextureModels = new List<AlternativeTextureModel>();
                        foreach (var (variation, texture) in variationCombination)
                        {
                            if (texture.Height <= 0)
                                throw new ContentPackException($"[{relativeFolderName}] Texture height <= 0 {texture}");
                            if (texture.Width <= 0)
                                throw new ContentPackException($"[{relativeFolderName}] Texture height <= 0 {texture}");

                            mutable_TextureModels.Add(
                                new AlternativeTextureModel()
                                {
                                    PackManifest = contentPackManifest,
                                    ForModel = modelIdentifier,
                                    Variation = variation.Id,
                                    Season = season,

                                    TextureRaw = new()
                                    {
                                        Bytes = texture.Bytes,
                                        Width = texture.Width,
                                        Height = texture.Height,
                                    },
                                    TextureHeight = file.TextureHeight,
                                    TextureWidth = file.TextureWidth,

                                    DisplayName = variation.Name,
                                    Keywords = [.. file.Keywords, .. variation.Keywords, contentPackManifest.UniqueID],

                                    IgnoreBuildingColorMask = file.IgnoreBuildingColorMask,
                                    // Animation = file.Animation,
                                }
                            );
                        }
                        mutable_files.Add(
                            new()
                            {
                                Id = (modelIdentifier, season),
                                Models = mutable_TextureModels,
                                IsImplicitSeason = file.Seasons.Count == 0,
                                TextureJsonPath = relativeFolderName,
                            }
                        );
                    }
                }
            }
            catch (JsonException error)
            {
                Monitor.Log(
                    PrettyPrint.InspectFormat(
                        $"{TAG:raw} Error loading {$"{relativeFolderName}/texture.json"}: {error.Message.Red():raw}"
                    ),
                    LogLevel.Warn
                );
            }
            catch (Exception error)
            {
                Monitor.Log($"{TAG} Error loading texture {relativeFolderName}: {error}".BrightBlack(), LogLevel.Warn);
            }
        }

        /// We should really only have one file per model+season pair,
        /// but implicit seasons do take a backseat to explicit ones
        foreach (var group in mutable_files.GroupBy(x => x.Id))
        {
            /// So we allow groups with 2 files, one implicit one explicit.
            /// That is the only case where we allow a group bigger than one.
            switch (group.ToList())
            {
                case []:
                    /// Huh
                    break;

                case [var file]:
                    foreach (var texture in file.Models)
                    {
                        yield return texture;
                    }
                    break;

                case [var file1, var file2]:
                    var fileToUse = (file1, file2) switch
                    {
                        ({ IsImplicitSeason: false } explicitFile, { IsImplicitSeason: false }) => explicitFile,
                        ({ IsImplicitSeason: true }, { IsImplicitSeason: false } explicitFile) => explicitFile,
                        _ => null,
                    };
                    if (fileToUse is not null)
                    {
                        foreach (var texture in fileToUse.Models)
                        {
                            yield return texture;
                        }
                    }
                    else
                    {
                        Monitor.Log(
                            $"{TAG} provided colliding textures for {group.Key.ToString().Green()}.".BrightBlack(),
                            LogLevel.Warn
                        );
                        Monitor.Log(PrettyPrint.InspectFormat($"{(file1, file2)}"), LogLevel.Info);
                    }

                    break;
                default:
                    /// OOofff
                    Monitor.Log(
                        $"{TAG} provided colliding textures for {group.Key.ToString().Green()}.".BrightBlack(),
                        LogLevel.Warn
                    );
                    break;
            }
        }
    }

    static IEnumerable<int> Infinite()
    {
        var i = 0;
        while (true)
            yield return i++;
    }

    internal static IEnumerable<ImageBytes> LoadTexturesFromSingleFile(
        AlternativeTextureFile textureFile,
        string textureFolderPath
    )
    {
        var filePath = Path.Combine(textureFolderPath, "texture.png");
        // Load in the single vertical texture
        // var tilesheetPath = contentPack
        //     .ModContent.GetInternalAssetName(Path.Combine(textureFolderPath, "texture.png"))
        //     .Name;
        // var texture = contentPack.ModContent.Load<Texture2D>(tilesheetPath);

        using var stream = File.OpenRead(filePath);
        using var bitmap =
            SKBitmap.Decode(stream)
            ?? throw new ContentPackTextureException($"This doesn't seem to be a valid PNG image.");

        if (bitmap.Height >= AlternativeTextureModel.MAX_TEXTURE_HEIGHT)
            throw new ContentPackTextureException("The texture has a height larger than 16384 pixels");

        if (textureFile.Type == TextureType.Decoration)
        {
            if (bitmap.Width != 256)
                throw new ContentPackTextureException(
                    "The required image width is 256 for Decoration types (wallpapers / floors)"
                );

            if (textureFile.ItemName == "Floor")
            {
                var texturesPerRow = 256 / 32;
                var rows = bitmap.Height / 32;
                foreach (var index in Enumerable.Range(0, rows * texturesPerRow))
                {
                    var left = (index % 8) * 32;
                    var top = (index / 8) * 32;
                    yield return LoadSubtextureSync(
                        bitmap,
                        new SKRectI(left: left, top: top, right: left + 32, bottom: top + 32),
                        new(left: 0, top: 0, right: 256, bottom: bitmap.Height)
                    );
                }
            }
            else if (textureFile.ItemName == "Wallpaper")
            {
                var texturesPerRow = 256 / 16;
                var rows = bitmap.Height / 48;
                foreach (var index in Enumerable.Range(0, rows * texturesPerRow))
                {
                    var left = (index % 16) * 16;
                    var top = (index / 16) * 48;
                    yield return LoadSubtextureSync(
                        bitmap,
                        new SKRectI(left: left, top: top, right: left + 16, bottom: top + 48),
                        new(left: 0, top: 0, right: 256, bottom: bitmap.Height)
                    );
                }
            }
            else
            {
                throw new ContentPackTextureException("Type = Decoration, but itemName is not Floor or Wallpaper");
            }
        }
        else
        {
            /// We are going to split soley based on the given TextureWidth and TextureHeight
            /// TODO I guess not, this seems to be part of the deal somehow...
            // if (texture.Width != textureFile.TextureWidth)
            //     throw new ContentPackTextureException(
            //         $"Texture file has different width than provided TextureWidth (defined: {textureFile.TextureWidth}, actual: {texture.Width})"
            //     );

            if (bitmap.Height % textureFile.TextureHeight != 0)
                throw new ContentPackTextureException(
                    $"Texture file height is not a multiple of provided TextureHeight (defined: {textureFile.TextureHeight}, actual: {bitmap.Height})"
                );

            var variantionsInTexture = bitmap.Height / textureFile.TextureHeight;

            foreach (var index in Enumerable.Range(0, variantionsInTexture))
            {
                yield return LoadSubtextureSync(
                    bitmap,
                    new(
                        left: 0,
                        top: textureFile.TextureHeight * index,
                        right: bitmap.Width,
                        bottom: (textureFile.TextureHeight * index) + textureFile.TextureHeight
                    )
                );
            }
        }
    }

    // internal static SKBitmap FlattenDecorationTexture(SKBitmap source, SKRectI sourceRect)
    // {
    //     var target = new SKBitmap(256, source.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
    //     target.Erase(SKColors.Transparent);

    //     using var canvas = new SKCanvas(target);
    //     var destRect = new SKRectI(0, 0, sourceRect.Width, sourceRect.Height);
    //     canvas.DrawBitmap(source, sourceRect, destRect);
    //     return target;
    // }

    // internal static SKBitmap ExtractSubSprite(SKBitmap source, SKRectI sourceRect)
    // {
    //     var target = new SKBitmap(sourceRect.Height, source.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
    //     using var canvas = new SKCanvas(target);
    //     var destRect = new SKRectI(0, 0, sourceRect.Width, sourceRect.Height);
    //     canvas.DrawBitmap(source, sourceRect, destRect);
    //     return target;
    // }

    public static ImageBytes LoadSubtextureSync(SKBitmap source, SKRectI sourceRect, SKRectI? destinationSize = null)
    {
        var targetInfo = new SKImageInfo(
            width: destinationSize?.Width ?? sourceRect.Width,
            height: destinationSize?.Height ?? sourceRect.Height,
            // The exact memory layout MonoGame requires (RGBA8888 + Premultiplied)
            colorType: SKColorType.Rgba8888,
            alphaType: SKAlphaType.Premul
        );

        using var targetBitmap = new SKBitmap(targetInfo);

        using var canvas = new SKCanvas(targetBitmap);
        canvas.Clear(SKColors.Transparent);

        var destRect = new SKRect(0, 0, sourceRect.Width, sourceRect.Height);
        canvas.DrawBitmap(source, sourceRect, destRect);

        // 6. Extract the perfectly formatted byte array
        return new ImageBytes()
        {
            Bytes = targetBitmap.Bytes,
            Width = targetBitmap.Width,
            Height = targetBitmap.Height,
        };
    }

    internal static IEnumerable<ImageBytes> LoadTexturesFromMultipleFiles(
        AlternativeTextureFile textureFile,
        string textureFolder
    )
    {
        var textureFileNames = Directory
            .GetFiles(textureFolder, "texture_*.png")
            .Select(t => Path.GetFileName(t))
            .Where(t => t.Any(char.IsDigit))
            .OrderBy(t => Int32.Parse(Regex.Match(t, @"\d+").Value));

        if (textureFileNames.Count() == 0)
            throw new ContentPackTextureException(
                "No associated texture.png or split textures (texture_1.png, texture_2.png, etc.)"
            );

        if (textureFile.Type is TextureType.Decoration)
            throw new ContentPackTextureException(
                "Split textures (texture_1.png, texture_2.png, etc.) are not allowed for Decoration types (wallpapers / floors)"
            );

        foreach (var textureFileName in textureFileNames)
        {
            using var stream = File.OpenRead(Path.Combine(textureFolder, textureFileName));
            using var bitmap =
                SKBitmap.Decode(stream)
                ?? throw new ContentPackTextureException($"This doesn't seem to be a valid PNG image.");

            yield return new ImageBytes()
            {
                Bytes = bitmap.Bytes,
                Width = bitmap.Width,
                Height = bitmap.Height,
            };
        }
    }
}
