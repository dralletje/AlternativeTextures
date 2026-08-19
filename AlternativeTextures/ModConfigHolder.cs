using System;
using System.Linq;
using AlternativeTextures.Framework.External.GenericModConfigMenu;
using AlternativeTextures.Framework.Interfaces;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace AlternativeTextures;

class ModConfigHolder(Mod mod)
{
    readonly IMonitor Monitor = mod.Monitor;
    readonly IManifest ModManifest = mod.ModManifest;
    readonly IModHelper Helper = mod.Helper;

    IGenericModConfigMenuApi? _genericModConfigMenuApi;

    public ModConfig ModConfig = new();

    IGenericModConfigMenuApi? GetGenericModConfigMenuApi()
    {
        if (Helper.ModRegistry.IsLoaded("spacechase0.GenericModConfigMenu"))
        {
            if (_genericModConfigMenuApi is { } modConfigMenu)
            {
                return modConfigMenu;
            }
            else
            {
                _genericModConfigMenuApi = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(
                    "spacechase0.GenericModConfigMenu"
                );
                if (_genericModConfigMenuApi is null)
                {
                    Monitor.Log("Failed to hook into spacechase0.GenericModConfigMenu.", LogLevel.Error);
                    return null;
                }
                else
                {
                    Monitor.Log("Successfully hooked into spacechase0.GenericModConfigMenu.", LogLevel.Debug);
                    return _genericModConfigMenuApi;
                }
            }
        }
        else
        {
            return null;
        }
    }

    public void RegisterWithGMC()
    {
        if (GetGenericModConfigMenuApi() is { } configApi)
        {
            configApi.Register(
                ModManifest,
                reset: () => ModConfig = new ModConfig(),
                save: () => Helper.WriteConfig(ModConfig)
            );

            // Register the standard settings
            configApi.AddSectionTitle(
                ModManifest,
                () => Helper.Translation.Get("config.section.use_random_textures_when")
            );
            configApi.AddBoolOption(
                ModManifest,
                () => ModConfig.UseRandomTexturesWhenSpawningArtifactSpots,
                value => ModConfig.UseRandomTexturesWhenSpawningArtifactSpots = value,
                () => Helper.Translation.Get("config.type_label.ArtifactSpot")
            );
            configApi.AddBoolOption(
                ModManifest,
                () => ModConfig.UseRandomTexturesWhenPlacingFlooring,
                value => ModConfig.UseRandomTexturesWhenPlacingFlooring = value,
                () => Helper.Translation.Get("config.type_label.Flooring")
            );
            configApi.AddBoolOption(
                ModManifest,
                () => ModConfig.UseRandomTexturesWhenPlacingFruitTree,
                value => ModConfig.UseRandomTexturesWhenPlacingFruitTree = value,
                () => Helper.Translation.Get("config.type_label.FruitTree")
            );
            configApi.AddBoolOption(
                ModManifest,
                () => ModConfig.UseRandomTexturesWhenPlacingTree,
                value => ModConfig.UseRandomTexturesWhenPlacingTree = value,
                () => Helper.Translation.Get("config.type_label.Tree")
            );
            configApi.AddBoolOption(
                ModManifest,
                () => ModConfig.UseRandomTexturesWhenPlacingHoeDirt,
                value => ModConfig.UseRandomTexturesWhenPlacingHoeDirt = value,
                () => Helper.Translation.Get("config.type_label.HoeDirt")
            );
            configApi.AddBoolOption(
                ModManifest,
                () => ModConfig.UseRandomTexturesWhenPlacingGrass,
                value => ModConfig.UseRandomTexturesWhenPlacingGrass = value,
                () => Helper.Translation.Get("config.type_label.Grass")
            );
            configApi.AddBoolOption(
                ModManifest,
                () => ModConfig.UseRandomTexturesWhenPlacingFurniture,
                value => ModConfig.UseRandomTexturesWhenPlacingFurniture = value,
                () => Helper.Translation.Get("config.type_label.Furniture")
            );
            configApi.AddBoolOption(
                ModManifest,
                () => ModConfig.UseRandomTexturesWhenPlacingObject,
                value => ModConfig.UseRandomTexturesWhenPlacingObject = value,
                () => Helper.Translation.Get("config.type_label.Object")
            );
            configApi.AddBoolOption(
                ModManifest,
                () => ModConfig.UseRandomTexturesWhenPlacingFarmAnimal,
                value => ModConfig.UseRandomTexturesWhenPlacingFarmAnimal = value,
                () => Helper.Translation.Get("config.type_label.FarmAnimal")
            );
            configApi.AddBoolOption(
                ModManifest,
                () => ModConfig.UseRandomTexturesWhenPlacingMonster,
                value => ModConfig.UseRandomTexturesWhenPlacingMonster = value,
                () => Helper.Translation.Get("config.type_label.Monster")
            );
            configApi.AddBoolOption(
                ModManifest,
                () => ModConfig.UseRandomTexturesWhenPlacingBuilding,
                value => ModConfig.UseRandomTexturesWhenPlacingBuilding = value,
                () => Helper.Translation.Get("config.type_label.Building")
            );

            var contentPacks = Helper.ContentPacks.GetOwned();
            string caret = Helper.Translation.Get("config.special.caret");
            // Create the page labels for each content pack's page
            configApi.AddSectionTitle(ModManifest, () => Helper.Translation.Get("config.section.content_pack"));

            // Add the content pack owner pages
            foreach (var contentPack in contentPacks)
            {
                configApi.AddPageLink(
                    ModManifest,
                    contentPack.Manifest.UniqueID,
                    () => String.Concat(caret, CleanContentPackNameForConfig(contentPack.Manifest.Name)),
                    () => contentPack.Manifest.Description
                );

                configApi.AddPage(
                    ModManifest,
                    contentPack.Manifest.UniqueID,
                    pageTitle: () => CleanContentPackNameForConfig(contentPack.Manifest.Name)
                );

                // Create a page label for each TextureType under this content pack
                configApi.AddSectionTitle(ModManifest, () => Helper.Translation.Get("config.section.categories"));
                foreach (
                    var textureType in AlternativeTextures
                        .textureManager.GetAllTextures()
                        .Where(t => t.Owner == contentPack.Manifest.UniqueID)
                        .Select(t => t.GetTextureType())
                        .Distinct()
                        .OrderBy(t => t)
                )
                {
                    configApi.AddPageLink(
                        ModManifest,
                        String.Concat(contentPack.Manifest.UniqueID, ".", textureType),
                        () => String.Concat(caret, Helper.Translation.Get($"config.type_label.{textureType}"))
                    );
                }

                // Create a page label for each model under this content pack
                foreach (
                    var model in AlternativeTextures
                        .textureManager.GetAllTextures()
                        .Where(t => t.Owner == contentPack.Manifest.UniqueID)
                        .OrderBy(t => t.GetTextureType())
                        .ThenBy(t => t.ItemName)
                )
                {
                    configApi.AddPage(
                        ModManifest,
                        String.Concat(contentPack.Manifest.UniqueID, ".", model.GetTextureType()),
                        pageTitle: () => Helper.Translation.Get($"config.type_label.{model.GetTextureType()}")
                    );
                    configApi.AddPageLink(
                        ModManifest,
                        model.GetId(),
                        () => String.Concat(caret, model.ItemName),
                        () =>
                            Helper.Translation.Get(
                                "config.model.description",
                                new
                                {
                                    textureType = model.GetTextureType(),
                                    season = String.IsNullOrEmpty(model.Season) ? "All" : model.Season,
                                    variations = model.GetVariations(),
                                }
                            )
                    );

                    for (var variation = 0; variation < model.GetVariations(); variation++)
                    {
                        string variationText = Helper.Translation.Get("config.model_single.name", new { variation });
                        // Add general description label
                        var description =
                            $"Type: {model.GetTextureType()}\nSeason(s): {(String.IsNullOrEmpty(model.Season) ? "All" : model.Season)}";
                        configApi.AddSectionTitle(
                            ModManifest,
                            () => variationText,
                            () =>
                                Helper.Translation.Get(
                                    "config.model_single.description",
                                    new
                                    {
                                        textureType = model.GetTextureType(),
                                        season = String.IsNullOrEmpty(model.Season) ? "All" : model.Season,
                                    }
                                )
                        );

                        // Add the reference image for the alternative texture
                        var sourceRect = new Rectangle(
                            0,
                            model.GetTextureOffset(variation),
                            model.TextureWidth,
                            model.TextureHeight
                        );
                        switch (model.GetTextureType())
                        {
                            case "Decoration":
                                var isFloor = model.ItemName.Equals("Floor", StringComparison.OrdinalIgnoreCase);
                                var decorationOffset = isFloor ? 8 : 16;
                                sourceRect = new Rectangle(
                                    (variation % decorationOffset) * model.TextureWidth,
                                    (variation / decorationOffset) * model.TextureHeight,
                                    model.TextureWidth,
                                    model.TextureHeight
                                );
                                break;
                        }

                        var scale = 4;
                        if (model.TextureHeight >= 64)
                        {
                            scale = 2;
                        }
                        if (model.TextureHeight >= 128)
                        {
                            scale = 1;
                        }

                        var modelTexture = model.GetTexture(variation);
                        configApi.AddImage(ModManifest, () => modelTexture, sourceRect, scale);

                        // Add our custom widget, which passes over the required data needed to flag the TextureId with the appropriate Variation
                        var textureWidget = new TextureWidget()
                        {
                            TextureId = model.GetId(),
                            Variation = variation,
                            Enabled = !ModConfig.IsTextureVariationDisabled(model.GetId(), variation),
                        };
                        configApi.AddComplexOption(
                            ModManifest,
                            () => Helper.Translation.Get("config.widget.enabled.name"),
                            textureWidget.Draw,
                            tooltip: () => Helper.Translation.Get("config.widget.enabled.description"),
                            beforeSave: () => textureWidget.BeforeSave(ModConfig)
                        );
                    }

                    configApi.AddPage(
                        ModManifest,
                        contentPack.Manifest.UniqueID,
                        pageTitle: () => CleanContentPackNameForConfig(contentPack.Manifest.Name)
                    );
                }

                configApi.AddPage(ModManifest, string.Empty);
            }
        }
    }

    string CleanContentPackNameForConfig(string contentPackName)
    {
        return contentPackName.Replace("[", String.Empty).Replace("]", String.Empty);
    }
}
