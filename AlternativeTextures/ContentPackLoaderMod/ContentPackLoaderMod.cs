using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AlternativeTextures.Framework;
using AlternativeTextures.Framework.Managers;
using AlternativeTextures.MetaFramework;
using Incubator;
using Incubator.MonoGame;
using Incubator.MonoGame.FlexibleTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AlternativeTextures.ContentPackLoaderMod;

public sealed class ContentPackException(string message) : Exception(message);

public sealed class ContentPackTextureException(string message) : Exception(message);

class ContentPackLoaderMod<TParent>(DralModContext<TParent, ContentPackLoaderMod<TParent>> context) : DralMod
    where TParent : HasMod<TextureManager>
{
    readonly IManifest ModManifest = context.ModManifest;
    readonly IModHelper Helper = context.Helper;

    public override IDisposable? Entry()
    {
        Helper.Events.GameLoop.GameLaunched += this.Load;

        return new ActionDisposable(() =>
        {
            Helper.Events.GameLoop.GameLaunched -= this.Load;
        });
    }

    public void Load(object? sender, GameLaunchedEventArgs e)
    {
        var textureManager = context.Parent.GetMod<TextureManager>();

        var collectiveLoadingStopwatch = Stopwatch.StartNew();

        // Load owned content packs
        foreach (var contentPack in Helper.ContentPacks.GetOwned())
        {
            var TAG = $"[{contentPack.Manifest.Name}]".Blue();
            Monitor.Log(
                $"Loading textures from pack: {contentPack.Manifest.Name} {contentPack.Manifest.Version} by {contentPack.Manifest.Author}",
                LogLevel.Debug
            );

            var individualLoadingStopwatch = Stopwatch.StartNew();
            try
            {
                var textureModels = LoadPackContents(contentPack).ToList();

                var shouldBeUnique = textureModels.GroupBy(model => (model.Owner, model.ForModel, model.Season));

                foreach (var matches in shouldBeUnique)
                {
                    var hashset = new HashSet<int>(matches.Select(x => x.Variation));
                    if (hashset.Count == matches.Count())
                    {
                        foreach (var match in matches)
                        {
                            textureManager.AddAlternativeTexture(match);
                        }
                    }
                    else
                    {
                        Console.Log($"{matches.Count()} textures found for {matches.Key}, not adding either");
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error loading content pack {contentPack.Manifest.Name}: {ex}", LogLevel.Error);
            }

            individualLoadingStopwatch.Stop();
            Monitor.Log(
                $"{TAG} finished loading in {Math.Round(individualLoadingStopwatch.ElapsedMilliseconds / 1000f, 2)} seconds",
                LogLevel.Trace
            );

            // AlternativeTextures.textureManager.AddAlternativeTexture(textureModel);
        }

        // Clear the wallpaper / flooring cache
        Helper.GameContent.InvalidateCache("Data/AdditionalWallpaperFlooring");

        collectiveLoadingStopwatch.Stop();
        Monitor.Log(
            $"Finished loading all content packs in {Math.Round(collectiveLoadingStopwatch.ElapsedMilliseconds / 1000f, 2)} seconds",
            LogLevel.Trace
        );
    }

    record TextureGroup
    {
        public required (ModelIdentifier, Season) Id { get; init; }
        public required List<AlternativeTextureModel> Models { get; init; }
        public required bool IsImplicitSeason { get; init; }
        public required string TextureJsonPath { get; init; }
    }

    internal static IEnumerable<AlternativeTextureModel> LoadPackContents(IContentPack contentPack)
    {
        var texturesRootFolder = Path.Combine(contentPack.DirectoryPath, "Textures");
        var textureFolders = new DirectoryInfo(texturesRootFolder).GetDirectories("*", SearchOption.AllDirectories);

        if (textureFolders.Length == 0)
            throw new ContentPackException("No folders found inside content pack");

        // var mutable_TextureModels = new List<AlternativeTextureModel>();
        var mutable_files = new List<TextureGroup>();
        var TAG = $"[{contentPack.Manifest.Name}]".Blue();

        // Load in the alternative textures
        foreach (var textureFolder in textureFolders)
        {
            try
            {
                if (File.Exists(Path.Combine(textureFolder.FullName, "texture.json")) is false)
                {
                    if (textureFolder.GetDirectories().Length == 0)
                        throw new ContentPackException("Texture folder is missing texture.json");

                    continue;
                }

                /// TODO Just use textureFolder.FullName?
                var relativeFolderName = textureFolder.FullName.Replace(contentPack.DirectoryPath, string.Empty)[1..];
                // var textureFolderPath = Path.Combine(parentFolderName, textureFolder.Name);

                // Console.Log($"textureFolderPath: {relativeFolderName}");
                // Console.Log($"textureFolder: {textureFolder.FullName}");
                var modelPath = Path.Combine(textureFolder.FullName, "texture.json");
                // Console.Log($"modelPath: {modelPath}");

                var text = File.ReadAllText(modelPath);
                var file =
                    JsonConvert.DeserializeObject<AlternativeTextureFile>(text)
                    ?? throw new ContentPackTextureException("Couldn't parse texture.json");
                // Console.Log($"ok: {file}");

                // var file =
                //     contentPack.ReadJsonFile<AlternativeTextureFile>(relativeFolderName)
                //     ?? throw new ContentPackTextureException("Couldn't parse texture.json");

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
                    ? LoadTexturesFromSingleFile(contentPack, file, relativeFolderName)
                    : LoadTexturesFromMultipleFiles(contentPack, file, relativeFolderName);

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
                            mutable_TextureModels.Add(
                                new AlternativeTextureModel()
                                {
                                    PackManifest = contentPack.Manifest,
                                    ForModel = modelIdentifier,
                                    Variation = variation.Id,
                                    Season = season,

                                    Texture = texture.Flatten(Game1.graphics.GraphicsDevice),
                                    TextureHeight = file.TextureHeight,
                                    TextureWidth = file.TextureWidth,

                                    DisplayName = variation.Name,
                                    Keywords = [.. file.Keywords, .. variation.Keywords, contentPack.Manifest.UniqueID],

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
            catch (Exception error)
            {
                Monitor.Log($"{TAG} Error loading texture {textureFolder}: {error}".BrightBlack(), LogLevel.Warn);
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

    internal static IEnumerable<ITexture> LoadTexturesFromSingleFile(
        IContentPack contentPack,
        AlternativeTextureFile textureFile,
        string textureFolderPath
    )
    {
        // Load in the single vertical texture
        var tilesheetPath = contentPack
            .ModContent.GetInternalAssetName(Path.Combine(textureFolderPath, "texture.png"))
            .Name;
        var texture = contentPack.ModContent.Load<Texture2D>(tilesheetPath);

        if (texture.Height >= AlternativeTextureModel.MAX_TEXTURE_HEIGHT)
            throw new ContentPackTextureException("The texture has a height larger than 16384 pixels");

        if (textureFile.Type == TextureType.Decoration)
        {
            if (texture.Width != 256)
                throw new ContentPackTextureException(
                    "The required image width is 256 for Decoration types (wallpapers / floors)"
                );

            if (textureFile.ItemName == "Floor")
            {
                var texturesPerRow = 256 / 32;
                var rows = texture.Height / 32;
                foreach (var index in Enumerable.Range(0, rows * texturesPerRow))
                {
                    yield return new IdentityTexture(
                        FlattenDecorationTexture(
                            Game1.graphics.GraphicsDevice,
                            new SubTexture(texture, new Rectangle((index % 8) * 32, (index / 8) * 32, 32, 32))
                        )
                    );
                }
            }
            else if (textureFile.ItemName == "Wallpaper")
            {
                var texturesPerRow = 256 / 16;
                var rows = texture.Height / 48;
                foreach (var index in Enumerable.Range(0, rows * texturesPerRow))
                {
                    yield return new IdentityTexture(
                        FlattenDecorationTexture(
                            Game1.graphics.GraphicsDevice,
                            new SubTexture(texture, new Rectangle((index % 16) * 16, (index / 16) * 48, 16, 48))
                        )
                    );
                    // yield return new ITexture()
                    // {
                    //     Texture = texture,
                    //     SourceRect = new Rectangle((index % 16) * 16, (index / 16) * 48, 16, 48),
                    // };
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

            if (texture.Height % textureFile.TextureHeight != 0)
                throw new ContentPackTextureException(
                    $"Texture file height is not a multiple of provided TextureHeight (defined: {textureFile.TextureHeight}, actual: {texture.Height})"
                );

            var variantionsInTexture = texture.Height / textureFile.TextureHeight;

            foreach (var index in Enumerable.Range(0, variantionsInTexture))
            {
                var bestDrawable = new SubTexture(
                    texture,
                    new Rectangle()
                    {
                        X = 0,
                        Y = textureFile.TextureHeight * index,
                        // Width = textureFile.TextureWidth,
                        Width = texture.Width,
                        Height = textureFile.TextureHeight,
                    }
                );

                /// TODO Don't flatten, once the rest of the code knows of ITexture
                yield return new IdentityTexture(bestDrawable.Flatten(Game1.graphics.GraphicsDevice));
            }
        }
    }

    internal static Texture2D FlattenDecorationTexture(GraphicsDevice gd, ITexture texture)
    {
        return Renderer.Render(
            gd,
            256,
            texture.Height,
            batch =>
            {
                batch.Begin(samplerState: SamplerState.PointClamp);
                batch.Draw(
                    texture,
                    new(0, 0, texture.Width, texture.Height),
                    new(0, 0, texture.Width, texture.Height),
                    Color.White
                );
                batch.End();
            }
        );
    }

    internal static IEnumerable<ITexture> LoadTexturesFromMultipleFiles(
        IContentPack contentPack,
        AlternativeTextureFile textureFile,
        string textureFolder
    )
    {
        var textureFileNames = Directory
            .GetFiles(Path.Combine(contentPack.DirectoryPath, textureFolder), "texture_*.png")
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
            var texture = contentPack.ModContent.Load<Texture2D>(Path.Combine(textureFolder, textureFileName));
            yield return new IdentityTexture(texture);
        }
    }
}
