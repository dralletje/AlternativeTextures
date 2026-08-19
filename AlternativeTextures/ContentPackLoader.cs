using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AlternativeTextures.Framework.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace AlternativeTextures;

class ContentPackLoader(Mod mod)
{
    readonly IMonitor Monitor = mod.Monitor;
    readonly IManifest ModManifest = mod.ModManifest;
    readonly IModHelper Helper = mod.Helper;

    public void Load()
    {
        var collectiveLoadingStopwatch = Stopwatch.StartNew();

        // Load owned content packs
        foreach (var contentPack in Helper.ContentPacks.GetOwned())
        {
            Monitor.Log(
                $"Loading textures from pack: {contentPack.Manifest.Name} {contentPack.Manifest.Version} by {contentPack.Manifest.Author}",
                LogLevel.Debug
            );

            var individualLoadingStopwatch = Stopwatch.StartNew();
            try
            {
                var textureFolders = new DirectoryInfo(
                    Path.Combine(contentPack.DirectoryPath, "Textures")
                ).GetDirectories("*", SearchOption.AllDirectories);
                if (textureFolders.Count() == 0)
                {
                    Monitor.Log(
                        $"No sub-folders found under Textures for the content pack {contentPack.Manifest.Name}!",
                        LogLevel.Warn
                    );
                    continue;
                }

                // Load in the alternative textures
                foreach (var textureFolder in textureFolders)
                {
                    if (!File.Exists(Path.Combine(textureFolder.FullName, "texture.json")))
                    {
                        if (textureFolder.GetDirectories().Count() == 0)
                        {
                            Monitor.Log(
                                $"Content pack {contentPack.Manifest.Name} is missing a texture.json under {textureFolder.Name}!",
                                LogLevel.Warn
                            );
                        }

                        continue;
                    }

                    var parentFolderName = textureFolder.Parent.FullName.Replace(
                        contentPack.DirectoryPath + Path.DirectorySeparatorChar,
                        String.Empty
                    );
                    var modelPath = Path.Combine(parentFolderName, textureFolder.Name, "texture.json");

                    var baseModel = contentPack.ReadJsonFile<AlternativeTextureModel>(modelPath);
                    baseModel.Owner = contentPack.Manifest.UniqueID;
                    baseModel.PackName = contentPack.Manifest.Name;
                    baseModel.Author = contentPack.Manifest.Author;

                    // Add to ItemId to CollectiveIds if ItemName is given or add to ItemName to CollectiveNames if ItemName is given
                    if (String.IsNullOrEmpty(baseModel.ItemId) is false)
                    {
                        baseModel.CollectiveIds.Add(baseModel.ItemId);
                    }
                    else if (String.IsNullOrEmpty(baseModel.ItemName) is false)
                    {
                        baseModel.CollectiveNames.Add(baseModel.ItemName);
                    }

                    // Handle SDV and framework related changes
                    var originalItemName = baseModel.ItemName;
                    if (baseModel.HandleNameChanges() is List<string> changedNames && changedNames.Count > 0)
                    {
                        foreach (var changedName in changedNames)
                        {
                            Monitor.Log(
                                $"The texture {baseModel.ItemName} from {contentPack.Manifest.Name} has an outdated ItemName that was handled automatically: {originalItemName} -> {changedName}",
                                LogLevel.Trace
                            );
                        }
                    }

                    var originalType = baseModel.Type;
                    if (baseModel.HandleTypeChanges())
                    {
                        Monitor.Log(
                            $"The texture {baseModel.ItemName} from {contentPack.Manifest.Name} has an outdated Type that was handled automatically: {originalType} -> {baseModel.Type}",
                            LogLevel.Trace
                        );
                    }

                    // Combine the two collective lists
                    var collectedCollective = new List<dynamic>();
                    foreach (var itemName in baseModel.CollectiveNames)
                    {
                        collectedCollective.Add(new { Name = itemName, IsId = false });
                    }
                    foreach (var itemId in baseModel.CollectiveIds)
                    {
                        collectedCollective.Add(new { Name = itemId, IsId = true });
                    }

                    // Attempt to add an instance of each season
                    var seasons = baseModel.Seasons;
                    for (var s = 0; s < 4; s++)
                    {
                        if ((seasons.Count() == 0 && s > 0) || (seasons.Count() > 0 && s >= seasons.Count()))
                        {
                            continue;
                        }

                        // Attempt to add each instance under CollectiveNames
                        foreach (var textureData in collectedCollective)
                        {
                            // Parse the model and assign it the content pack's owner
                            var textureModel = baseModel.ShallowCopy();

                            // Set the ItemName or ItemId depending on IsId flag
                            if (textureData.IsId is true)
                            {
                                textureModel.ItemId = textureData.Name;
                            }
                            else
                            {
                                // Override Grass Alternative Texture pack ItemName to always be Grass, in order to be compatible with translations
                                textureModel.ItemName =
                                    textureModel.Type.ToString() == "Grass" ? "Grass" : textureData.Name;
                            }

                            // Verify that ItemName or ItemNames is given
                            if (collectedCollective.Count() == 0)
                            {
                                Monitor.Log(
                                    $"Unable to add alternative texture for {textureModel.Owner}: Missing the ItemName, ItemId, CollectiveNames or CollectiveIds property! See the log for additional details.",
                                    LogLevel.Warn
                                );
                                Monitor.Log(
                                    $"Unable to add alternative texture for {textureModel.Owner}: Missing the ItemName, ItemId, CollectiveNames or CollectiveIds property found in the following path: {textureFolder.FullName}",
                                    LogLevel.Trace
                                );
                                continue;
                            }

                            // Add the UniqueId to the top-level Keywords
                            textureModel.Keywords.Add(contentPack.Manifest.UniqueID);

                            // Add the top-level Keywords to any ManualVariations.Keywords
                            foreach (var variation in textureModel.ManualVariations)
                            {
                                variation.Keywords.AddRange(textureModel.Keywords);
                            }

                            // Set the season (if any)
                            textureModel.Season = seasons.Count() == 0 ? String.Empty : seasons[s];

                            // Set the ModelName and TextureId
                            textureModel.ModelName = String.IsNullOrEmpty(textureModel.Season)
                                ? String.Concat(textureModel.GetTextureType(), "_", textureModel.ItemName)
                                : String.Concat(
                                    textureModel.GetTextureType(),
                                    "_",
                                    textureModel.ItemName,
                                    "_",
                                    textureModel.Season
                                );
                            textureModel.TextureId = String.Concat(textureModel.Owner, ".", textureModel.ModelName);

                            // Verify we are given a texture and if so, track it
                            if (!File.Exists(Path.Combine(textureFolder.FullName, "texture.png")))
                            {
                                // No texture.png found, may be using split texture files (texture_1.png, texture_2.png, etc.)
                                var textureFilePaths = Directory
                                    .GetFiles(textureFolder.FullName, "texture_*.png")
                                    .Select(t => Path.GetFileName(t))
                                    .Where(t => t.Any(char.IsDigit))
                                    .OrderBy(t => Int32.Parse(Regex.Match(t, @"\d+").Value));

                                if (textureFilePaths.Count() == 0)
                                {
                                    Monitor.Log(
                                        $"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: No associated texture.png or split textures (texture_1.png, texture_2.png, etc.) given. See the log for additional details.",
                                        LogLevel.Warn
                                    );
                                    Monitor.Log(
                                        $"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: No associated texture.png or split textures (texture_1.png, texture_2.png, etc.) found in the following path: {textureFolder.FullName}",
                                        LogLevel.Trace
                                    );
                                    continue;
                                }
                                else if (textureModel.IsDecoration())
                                {
                                    Monitor.Log(
                                        $"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: Split textures (texture_1.png, texture_2.png, etc.) are not allowed for Decoration types (wallpapers / floors). See the log for additional details.",
                                        LogLevel.Warn
                                    );
                                    Monitor.Log(
                                        $"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: Split textures (texture_1.png, texture_2.png, etc.) are not allowed for Decoration types (wallpapers / floors). Located in the following path: {textureFolder.FullName}",
                                        LogLevel.Trace
                                    );
                                    continue;
                                }

                                if (textureModel.GetVariations() < textureFilePaths.Count())
                                {
                                    Monitor.Log(
                                        $"Warning for alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: There are less variations specified in texture.json than split textures files. See the log for additional details.",
                                        LogLevel.Warn
                                    );
                                    Monitor.Log(
                                        $"Warning for alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: There are less variations specified in texture.json than split textures files found in the following path: {textureFolder.FullName}",
                                        LogLevel.Trace
                                    );
                                }
                                else if (textureModel.IsManualVariationsValid() is false)
                                {
                                    Monitor.Log(
                                        $"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: ManualVariations is used but does not start with ID == 0 (the propery should be zero-indexed). See the log for additional details.",
                                        LogLevel.Warn
                                    );
                                    Monitor.Log(
                                        $"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: ManualVariations is used but does not start with ID == 0 (the propery should be zero-indexed). Adjust the ID order so that it starts with ID = 0. Located in the following path: {textureFolder.FullName}",
                                        LogLevel.Trace
                                    );
                                    continue;
                                }

                                // Load in the first texture_#.png to get its dimensions for creating stitchedTexture
                                if (
                                    !StitchTexturesToModel(
                                        textureModel,
                                        contentPack,
                                        Path.Combine(parentFolderName, textureFolder.Name),
                                        textureFilePaths.Take(textureModel.GetVariations())
                                    )
                                )
                                {
                                    continue;
                                }

                                textureModel.TileSheetPath = contentPack
                                    .ModContent.GetInternalAssetName(
                                        Path.Combine(parentFolderName, textureFolder.Name, textureFilePaths.First())
                                    )
                                    .Name;
                            }
                            else
                            {
                                // Load in the single vertical texture
                                textureModel.TileSheetPath = contentPack
                                    .ModContent.GetInternalAssetName(
                                        Path.Combine(parentFolderName, textureFolder.Name, "texture.png")
                                    )
                                    .Name;
                                var singularTexture = contentPack.ModContent.Load<Texture2D>(
                                    textureModel.TileSheetPath
                                );
                                if (singularTexture.Height >= AlternativeTextureModel.MAX_TEXTURE_HEIGHT)
                                {
                                    Monitor.Log(
                                        $"Unable to add alternative texture for {textureModel.Owner}: The texture {textureModel.TextureId} has a height larger than 16384!\nPlease split it into individual textures (e.g. texture_0.png, texture_1.png, etc.) to resolve this issue. See the log for additional details.",
                                        LogLevel.Warn
                                    );
                                    Monitor.Log(
                                        $"Unable to add alternative texture for {textureModel.Owner}: The texture {textureModel.TextureId} has a height larger than 16384!\nPlease split it into individual textures (e.g. texture_0.png, texture_1.png, etc.) to resolve this issue. Located in the following path: {textureFolder.FullName}",
                                        LogLevel.Trace
                                    );
                                    continue;
                                }
                                else if (textureModel.IsDecoration())
                                {
                                    if (singularTexture.Width < 256)
                                    {
                                        Monitor.Log(
                                            $"Unable to add alternative texture for {textureModel.ItemName} from {contentPack.Manifest.Name}: The required image width is 256 for Decoration types (wallpapers / floors). Please correct the image's width manually. See the log for additional details.",
                                            LogLevel.Warn
                                        );
                                        Monitor.Log(
                                            $"Unable to add alternative texture for {textureModel.ItemName} from {contentPack.Manifest.Name}: The required image width is 256 for Decoration types (wallpapers / floors). Please correct the image's width manually at the following path: {textureFolder.FullName}",
                                            LogLevel.Trace
                                        );
                                        continue;
                                    }

                                    textureModel.Textures[0] = singularTexture;
                                }
                                else if (
                                    !SplitVerticalTexturesToModel(
                                        textureModel,
                                        contentPack.Manifest.Name,
                                        singularTexture
                                    )
                                )
                                {
                                    continue;
                                }
                            }

                            // Track the texture model
                            AlternativeTextures.textureManager.AddAlternativeTexture(textureModel);

                            // Log it
                            if (AlternativeTextures.modConfig.OutputTextureDataToLog)
                            {
                                Monitor.Log(textureModel.ToString(), LogLevel.Trace);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error loading content pack {contentPack.Manifest.Name}: {ex}", LogLevel.Error);
            }

            individualLoadingStopwatch.Stop();
            Monitor.Log(
                $"[{contentPack.Manifest.Name}] finished loading in {Math.Round(individualLoadingStopwatch.ElapsedMilliseconds / 1000f, 2)} seconds",
                LogLevel.Trace
            );
        }

        // Clear the wallpaper / flooring cache
        Helper.GameContent.InvalidateCache("Data/AdditionalWallpaperFlooring");

        collectiveLoadingStopwatch.Stop();
        Monitor.Log(
            $"Finished loading all content packs in {Math.Round(collectiveLoadingStopwatch.ElapsedMilliseconds / 1000f, 2)} seconds",
            LogLevel.Trace
        );
    }

    internal static bool SplitVerticalTexturesToModel(
        AlternativeTextureModel textureModel,
        string contentPackName,
        Texture2D verticalTexture
    )
    {
        try
        {
            for (var v = 0; v < textureModel.GetVariations(); v++)
            {
                var extractRectangle = new Rectangle(
                    0,
                    textureModel.TextureHeight * v,
                    verticalTexture.Width,
                    textureModel.TextureHeight
                );
                Color[] extractPixels = new Color[extractRectangle.Width * extractRectangle.Height];

                if (verticalTexture.Bounds.Contains(extractRectangle) is false)
                {
                    var maxVariationsPossible = verticalTexture.Height / textureModel.TextureHeight;

                    AlternativeTextures.monitor.Log(
                        $"Unable to add alternative texture for item {textureModel.ItemName} from {contentPackName}: More variations specified ({textureModel.GetVariations()}) than given ({maxVariationsPossible})",
                        LogLevel.Warn
                    );
                    return false;
                }

                // Get the required pixels
                verticalTexture.GetData(0, extractRectangle, extractPixels, 0, extractPixels.Length);

                // Set the required pixels
                var extractedTexture = new Texture2D(
                    Game1.graphics.GraphicsDevice,
                    extractRectangle.Width,
                    extractRectangle.Height
                );
                extractedTexture.SetData(extractPixels);

                textureModel.Textures[v] = extractedTexture;
            }
        }
        catch (Exception exception)
        {
            AlternativeTextures.monitor.Log(
                $"Unable to add alternative texture for item {textureModel.ItemName} from {contentPackName}: Unhandled framework error: {exception}",
                LogLevel.Warn
            );
            return false;
        }

        return true;
    }

    bool StitchTexturesToModel(
        AlternativeTextureModel textureModel,
        IContentPack contentPack,
        string rootPath,
        IEnumerable<string> textureFilePaths
    )
    {
        var baseTexture = contentPack.ModContent.Load<Texture2D>(Path.Combine(rootPath, textureFilePaths.First()));

        // If there is only one split texture file, skip the rest of the logic to avoid issues
        if (textureFilePaths.Count() == 1 || textureModel.GetVariations() == 1)
        {
            if (textureModel.GetVariations() == 1 && textureFilePaths.Count() > 1)
            {
                Monitor.Log(
                    $"Detected more split textures ({textureFilePaths.Count()}) than specified variations ({textureModel.GetVariations()}) for {textureModel.TextureId} from {contentPack.Manifest.Name}",
                    LogLevel.Warn
                );
            }

            textureModel.Textures[0] = baseTexture;
            return true;
        }

        try
        {
            var variation = 0;
            foreach (var textureFilePath in textureFilePaths)
            {
                var splitTexture = contentPack.ModContent.Load<Texture2D>(Path.Combine(rootPath, textureFilePath));
                textureModel.Textures[variation] = splitTexture;

                variation++;
            }
        }
        catch (Exception exception)
        {
            Monitor.Log(
                $"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: Unhandled framework error: {exception}",
                LogLevel.Warn
            );
            return false;
        }

        return true;
    }
}
