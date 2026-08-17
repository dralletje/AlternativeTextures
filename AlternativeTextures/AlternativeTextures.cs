using AlternativeTextures.Framework;
using AlternativeTextures.Framework.External.ContentPatcher;
using AlternativeTextures.Framework.External.GenericModConfigMenu;
using AlternativeTextures.Framework.Interfaces.API;
using AlternativeTextures.Framework.Managers;
using AlternativeTextures.Framework.Models;
using AlternativeTextures.Framework.Patches;
using AlternativeTextures.Framework.Patches.Buildings;
using AlternativeTextures.Framework.Patches.Entities;
using AlternativeTextures.Framework.Patches.GameLocations;
using AlternativeTextures.Framework.Patches.SpecialObjects;
using AlternativeTextures.Framework.Patches.StandardObjects;
using AlternativeTextures.Framework.Patches.Tools;
using AlternativeTextures.Framework.Utilities;
using AlternativeTextures.Framework.Utilities.Extensions;
using AlternativeTextures.Tools;
using ConsoleLog;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.GameData;
using StardewValley.GameData.GiantCrops;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AlternativeTextures
{
    public class AlternativeTextures : Mod
    {
        internal const string PAINTPAIL = "(F)PeacefulEnd.AlternativeTexturesContentPatcher_PaintPail";
        internal const string PAINT_BRUSH_FLAG = PaintBrushTool.PAINT_BRUSH_FLAG;
        internal const string PAINT_BRUSH_SCALE = PaintBrushTool.PAINT_BRUSH_SCALE;

        // Core modData keys
        internal const string TEXTURE_TOKEN_HEADER = "AlternativeTextures/Textures/";
        internal const string TOOL_TOKEN_HEADER = "AlternativeTextures/Tools/";
        internal const string DEFAULT_OWNER = "Stardew.Default";
        internal const string ENABLED_SPRAY_CAN_TEXTURES = "Stardew.Default";

        // Tool related keys
        internal const string PAINT_BUCKET_FLAG = "AlternativeTextures.PaintBucketFlag";
        internal const string OLD_PAINT_BUCKET_FLAG = "AlternativeTexturesPaintBucketFlag";
        internal const string SCISSORS_FLAG = "AlternativeTextures.ScissorsFlag";
        internal const string SPRAY_CAN_FLAG = "AlternativeTextures.SprayCanFlag";
        internal const string SPRAY_CAN_RARE = "AlternativeTextures.SprayCanRare";
        internal const string SPRAY_CAN_RADIUS = "AlternativeTextures.SprayCanRadius";
        internal const string CATALOGUE_FLAG = "AlternativeTextures.CatalogueFlag";

        // Mod ID const
        internal const string MOD_ID = "PeacefulEnd.AlternativeTextures";

        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static Multiplayer multiplayer;
        internal static ModConfig modConfig;

        // Managers
        internal static TextureManager textureManager;
        internal static MessageManager messageManager;
        internal static ApiManager apiManager;
        internal static ToolManager toolManager;

        // Utilities
        internal static FpsCounter fpsCounter;
        private static Api _api;

        // Tool related variables
        private Point _lastSprayCanTile = new Point();

        // Debugging flags
        private bool _displayFPS = false;

        private CustomToolPlugin? customToolPlugin;

        public override void Entry(IModHelper helper)
        {
            // Set up the monitor, helper and multiplayer
            monitor = Monitor;
            modHelper = helper;
            multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

            // Setup our managers
            textureManager = new TextureManager(monitor, helper);
            messageManager = new MessageManager(monitor, helper, ModManifest.UniqueID);
            apiManager = new ApiManager(monitor);
            toolManager = new ToolManager(helper);

            // Setup our utilities
            fpsCounter = new FpsCounter();
            _api = new Api(this);

            this.customToolPlugin = new CustomToolPlugin(helper);

            this.customToolPlugin.Start();

            // Load our Harmony patches
            try
            {
                var harmony = new Harmony(this.ModManifest.UniqueID);

                // Apply texture override related patches
                new GameLocationPatch(monitor, helper).Apply(harmony);
                new ObjectPatch(monitor, helper).Apply(harmony);
                new FencePatch(monitor, helper).Apply(harmony);
                new HoeDirtPatch(monitor, helper).Apply(harmony);
                new CropPatch(monitor, helper).Apply(harmony);
                new GiantCropPatch(monitor, helper).Apply(harmony);
                new GrassPatch(monitor, helper).Apply(harmony);
                new TreePatch(monitor, helper).Apply(harmony);
                new FruitTreePatch(monitor, helper).Apply(harmony);
                new ResourceClumpPatch(monitor, helper).Apply(harmony);
                new BushPatch(monitor, helper).Apply(harmony);
                new FlooringPatch(monitor, helper).Apply(harmony);
                new FurniturePatch(monitor, helper).Apply(harmony);
                new BedFurniturePatch(monitor, helper).Apply(harmony);
                new FishTankFurniturePatch(monitor, helper).Apply(harmony);

                // Start of special objects
                new ChestPatch(monitor, helper).Apply(harmony);
                new CrabPotPatch(monitor, helper).Apply(harmony);
                new IndoorPotPatch(monitor, helper).Apply(harmony);
                new PhonePatch(monitor, helper).Apply(harmony);
                new TorchPatch(monitor, helper).Apply(harmony);
                new WoodChipperPatch(monitor, helper).Apply(harmony);

                // Start of entity patches
                new CharacterPatch(monitor, helper).Apply(harmony);
                new ChildPatch(monitor, helper).Apply(harmony);
                new FarmAnimalPatch(monitor, helper).Apply(harmony);
                new HorsePatch(monitor, helper).Apply(harmony);
                new PetPatch(monitor, helper).Apply(harmony);
                new MonsterPatch(monitor, helper).Apply(harmony);

                // Start of building patches
                new BuildingPatch(monitor, helper).Apply(harmony);
                new ShippingBinPatch(monitor, helper).Apply(harmony);

                // Start of location patches
                new GameLocationPatch(monitor, helper).Apply(harmony);

                // Paint tool related patches
                new ToolPatch(monitor, helper).Apply(harmony);
            }
            catch (Exception e)
            {
                Monitor.Log($"Issue with Harmony patching: {e}", LogLevel.Error);
                return;
            }

            // Add in our debug commands
            helper.ConsoleCommands.Add("at_spawn_monsters", "Spawns monster(s) of a specified type and quantity at the current location.\n\nUsage: at_spawn_monsters [MONSTER_ID] (QUANTITY)", this.DebugSpawnMonsters);
            helper.ConsoleCommands.Add("at_spawn_gc", "Spawns a giant crop based given harvest product id (e.g. Melon == 254).\n\nUsage: at_spawn_gc [HARVEST_ID]", this.DebugSpawnGiantCrop);
            helper.ConsoleCommands.Add("at_spawn_rc", "Spawns a resource clump based given resource name (e.g. Stump).\n\nUsage: at_spawn_rc [RESOURCE_NAME]", this.DebugSpawnResourceClump);
            helper.ConsoleCommands.Add("at_spawn_child", "Spawns a child. Potentially buggy / gamebreaking, do not use. \n\nUsage: at_spawn_child [AGE] [IS_MALE] [SKIN_TONE]", this.DebugSpawnChild);
            helper.ConsoleCommands.Add("at_set_age", "Sets age for all children in location. Potentially buggy / gamebreaking, do not use. \n\nUsage: at_set_age [AGE]", this.DebugSetAge);
            helper.ConsoleCommands.Add("at_display_fps", "Displays FPS counter. Use again to disable. \n\nUsage: at_display_fps", delegate { _displayFPS = !_displayFPS; });
            helper.ConsoleCommands.Add("at_paint_shop", "Shows the carpenter shop with the paint bucket for sale.\n\nUsage: at_paint_shop", this.DebugShowPaintShop);
            helper.ConsoleCommands.Add("at_set_object_texture", "Sets the texture of the object below the player.\n\nUsage: at_set_object_texture [TEXTURE_ID] (VARIATION_NUMBER) (SEASON)", this.DebugSetTexture);
            helper.ConsoleCommands.Add("at_clear_texture", "Clears the texture of the object below the player.\n\nUsage: at_clear_texture", this.DebugClearTexture);
            helper.ConsoleCommands.Add("at_reload", "Reloads all Alternative Texture content packs.\n\nUsage: at_reload", delegate { this.LoadContentPacks(); });

            // Hook into GameLoop events
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

            // Hook into Input events
            helper.Events.Input.ButtonsChanged += OnButtonChanged;

            // Hook into Display events
            helper.Events.Display.Rendered += OnDisplayRendered;

            // Hook into the Content events
            helper.Events.Content.AssetRequested += OnContentAssetRequested;
            helper.Events.Content.AssetReady += OnContentAssetReady;

            // Hook into Multiplayer events
            helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        }

        private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID == ModManifest.UniqueID)
            {
                messageManager.HandleIncomingMessage(e);
            }
        }

        private void OnContentAssetReady(object? sender, AssetReadyEventArgs e)
        {
            var asset = e.Name;
            if (textureManager.GetTextureByToken(asset.Name) is Texture2D texture && texture is not null)
            {
                var loadedTexture = Helper.GameContent.Load<Texture2D>(asset.Name);

                textureManager.UpdateTexture(asset.Name, loadedTexture);
            }
        }

        private void OnContentAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.DataType == typeof(Texture2D))
            {
                var asset = e.Name;
                if (textureManager.GetModelByToken(asset.Name) is TokenModel tokenModel && tokenModel is not null)
                {
                    var originalTexture = tokenModel.AlternativeTexture.GetTexture(tokenModel.Variation);
                    var clonedTexture = originalTexture.CreateSelectiveCopy(Game1.graphics.GraphicsDevice, new Rectangle(0, 0, originalTexture.Width, originalTexture.Height));
                    e.LoadFrom(() => clonedTexture, AssetLoadPriority.Exclusive);
                }
                else if (toolManager.toolKeyToData.ContainsKey(asset.Name))
                {
                    e.LoadFromModFile<Texture2D>(toolManager.toolKeyToData[asset.Name], AssetLoadPriority.Exclusive);
                }
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/AdditionalWallpaperFlooring") && textureManager.GetValidTextureNamesWithSeason().Count > 0)
            {
                e.Edit(asset =>
                {
                    List<ModWallpaperOrFlooring> moddedDecorations = asset.GetData<List<ModWallpaperOrFlooring>>();

                    foreach (var textureModel in textureManager.GetAllTextures().Where(t => t.IsDecoration() && !moddedDecorations.Any(d => d.Id == t.GetId())))
                    {
                        var decoration = new ModWallpaperOrFlooring()
                        {
                            Id = textureModel.GetId(),
                            Texture = $"{AlternativeTextures.TEXTURE_TOKEN_HEADER}{textureModel.GetTokenId()}",
                            IsFlooring = String.Equals(textureModel.ItemName, "Floor", StringComparison.OrdinalIgnoreCase),
                            Count = textureModel.GetVariations()
                        };

                        moddedDecorations.Add(decoration);
                    }
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Tools"))
            {
                e.Edit(toolManager.Edit_DataTools, AssetEditPriority.Early);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops"))
            {
                e.Edit(toolManager.Edit_DataShops, AssetEditPriority.Early);
            }
        }

        private void OnDisplayRendered(object? sender, RenderedEventArgs e)
        {
            if (!_displayFPS)
            {
                return;
            }

            fpsCounter.OnRendered(sender, e);
        }

        private void OnButtonChanged(object? sender, ButtonsChangedEventArgs e)
        {
            if (Game1.activeClickableMenu is null && Game1.player.CurrentTool is GenericTool tool && e.Held.Contains(SButton.MouseLeft))
            {
                if (tool.modData.ContainsKey(CATALOGUE_FLAG))
                {
                    ToolPatch.UseTextureCatalogue(Game1.player);
                }
            }
        }

        public override object GetApi()
        {
            return _api;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // Set our default configuration file
            modConfig = Helper.ReadConfig<ModConfig>();

            if (Helper.ModRegistry.IsLoaded("spacechase0.MoreGiantCrops"))
            {
                apiManager.HookIntoMoreGiantCrops(Helper);
            }

            if (Helper.ModRegistry.IsLoaded("spacechase0.DynamicGameAssets"))
            {
                apiManager.HookIntoDynamicGameAssets(Helper);
            }

            if (Helper.ModRegistry.IsLoaded("Pathoschild.ContentPatcher") && apiManager.HookIntoContentPatcher(Helper))
            {
                apiManager.GetContentPatcherApi().RegisterToken(ModManifest, "Textures", new TextureToken(textureManager, toolManager));
                apiManager.GetContentPatcherApi().RegisterToken(ModManifest, "Tools", new ToolToken(textureManager, toolManager));
            }

            // Load any owned content packs
            this.LoadContentPacks();

            Monitor.Log($"Finished loading Alternative Textures content packs", LogLevel.Debug);

            // Hook into GMCM, if applicable
            if (Helper.ModRegistry.IsLoaded("spacechase0.GenericModConfigMenu") && apiManager.HookIntoGenericModConfigMenu(Helper))
            {
                var configApi = apiManager.GetGenericModConfigMenuApi();
                configApi.Register(ModManifest, () => modConfig = new ModConfig(), () => Helper.WriteConfig(modConfig));

                // Register the standard settings
                configApi.AddSectionTitle(ModManifest, () => Helper.Translation.Get("config.section.use_random_textures_when"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenSpawningArtifactSpots, value => modConfig.UseRandomTexturesWhenSpawningArtifactSpots = value, () => Helper.Translation.Get("config.type_label.ArtifactSpot"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenPlacingFlooring, value => modConfig.UseRandomTexturesWhenPlacingFlooring = value, () => Helper.Translation.Get("config.type_label.Flooring"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenPlacingFruitTree, value => modConfig.UseRandomTexturesWhenPlacingFruitTree = value, () => Helper.Translation.Get("config.type_label.FruitTree"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenPlacingTree, value => modConfig.UseRandomTexturesWhenPlacingTree = value, () => Helper.Translation.Get("config.type_label.Tree"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenPlacingHoeDirt, value => modConfig.UseRandomTexturesWhenPlacingHoeDirt = value, () => Helper.Translation.Get("config.type_label.HoeDirt"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenPlacingGrass, value => modConfig.UseRandomTexturesWhenPlacingGrass = value, () => Helper.Translation.Get("config.type_label.Grass"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenPlacingFurniture, value => modConfig.UseRandomTexturesWhenPlacingFurniture = value, () => Helper.Translation.Get("config.type_label.Furniture"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenPlacingObject, value => modConfig.UseRandomTexturesWhenPlacingObject = value, () => Helper.Translation.Get("config.type_label.Object"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenPlacingFarmAnimal, value => modConfig.UseRandomTexturesWhenPlacingFarmAnimal = value, () => Helper.Translation.Get("config.type_label.FarmAnimal"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenPlacingMonster, value => modConfig.UseRandomTexturesWhenPlacingMonster = value, () => Helper.Translation.Get("config.type_label.Monster"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenPlacingBuilding, value => modConfig.UseRandomTexturesWhenPlacingBuilding = value, () => Helper.Translation.Get("config.type_label.Building"));

                var contentPacks = Helper.ContentPacks.GetOwned();
                string caret = Helper.Translation.Get("config.special.caret");
                // Create the page labels for each content pack's page
                configApi.AddSectionTitle(ModManifest, () => Helper.Translation.Get("config.section.content_pack"));

                // Add the content pack owner pages
                foreach (var contentPack in contentPacks)
                {
                    configApi.AddPageLink(ModManifest, contentPack.Manifest.UniqueID, () => String.Concat(caret, CleanContentPackNameForConfig(contentPack.Manifest.Name)), () => contentPack.Manifest.Description);

                    configApi.AddPage(ModManifest, contentPack.Manifest.UniqueID, pageTitle: () => CleanContentPackNameForConfig(contentPack.Manifest.Name));

                    // Create a page label for each TextureType under this content pack
                    configApi.AddSectionTitle(ModManifest, () => Helper.Translation.Get("config.section.categories"));
                    foreach (var textureType in textureManager.GetAllTextures().Where(t => t.Owner == contentPack.Manifest.UniqueID).Select(t => t.GetTextureType()).Distinct().OrderBy(t => t))
                    {
                        configApi.AddPageLink(ModManifest, String.Concat(contentPack.Manifest.UniqueID, ".", textureType), () => String.Concat(caret, Helper.Translation.Get($"config.type_label.{textureType}")));
                    }

                    // Create a page label for each model under this content pack
                    foreach (var model in textureManager.GetAllTextures().Where(t => t.Owner == contentPack.Manifest.UniqueID).OrderBy(t => t.GetTextureType()).ThenBy(t => t.ItemName))
                    {
                        configApi.AddPage(ModManifest, String.Concat(contentPack.Manifest.UniqueID, ".", model.GetTextureType()), pageTitle: () => Helper.Translation.Get($"config.type_label.{model.GetTextureType()}"));
                        configApi.AddPageLink(
                            ModManifest, model.GetId(),
                            () => String.Concat(caret, model.ItemName),
                            () => Helper.Translation.Get("config.model.description", new
                            {
                                textureType = model.GetTextureType(),
                                season = String.IsNullOrEmpty(model.Season) ? "All" : model.Season,
                                variations = model.GetVariations()
                            })
                        );


                        for (int variation = 0; variation < model.GetVariations(); variation++)
                        {
                            string variationText = Helper.Translation.Get("config.model_single.name", new { variation });
                            // Add general description label
                            var description = $"Type: {model.GetTextureType()}\nSeason(s): {(String.IsNullOrEmpty(model.Season) ? "All" : model.Season)}";
                            configApi.AddSectionTitle(
                                ModManifest,
                                () => variationText,
                                () => Helper.Translation.Get("config.model_single.description",
                                    new
                                    {
                                        textureType = model.GetTextureType(),
                                        season = String.IsNullOrEmpty(model.Season) ? "All" : model.Season,
                                    }
                                )
                            );

                            // Add the reference image for the alternative texture
                            var sourceRect = new Rectangle(0, model.GetTextureOffset(variation), model.TextureWidth, model.TextureHeight);
                            switch (model.GetTextureType())
                            {
                                case "Decoration":
                                    var isFloor = model.ItemName.Equals("Floor", StringComparison.OrdinalIgnoreCase);
                                    var decorationOffset = isFloor ? 8 : 16;
                                    sourceRect = new Rectangle((variation % decorationOffset) * model.TextureWidth, (variation / decorationOffset) * model.TextureHeight, model.TextureWidth, model.TextureHeight);
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
                            var textureWidget = new TextureWidget() { TextureId = model.GetId(), Variation = variation, Enabled = !modConfig.IsTextureVariationDisabled(model.GetId(), variation) };
                            configApi.AddComplexOption(
                                ModManifest,
                                () => Helper.Translation.Get("config.widget.enabled.name"),
                                textureWidget.Draw,
                                tooltip: () => Helper.Translation.Get("config.widget.enabled.description"),
                                beforeSave: () => textureWidget.BeforeSave(modConfig)
                            );
                        }

                        configApi.AddPage(ModManifest, contentPack.Manifest.UniqueID, pageTitle: () => CleanContentPackNameForConfig(contentPack.Manifest.Name));
                    }

                    configApi.AddPage(ModManifest, String.Empty);
                }
            }
        }

        private void LoadContentPacks()
        {
            Stopwatch collectiveLoadingStopwatch = Stopwatch.StartNew();

            // Load owned content packs
            foreach (IContentPack contentPack in Helper.ContentPacks.GetOwned())
            {
                Monitor.Log($"Loading textures from pack: {contentPack.Manifest.Name} {contentPack.Manifest.Version} by {contentPack.Manifest.Author}", LogLevel.Debug);

                Stopwatch individualLoadingStopwatch = Stopwatch.StartNew();
                try
                {
                    var textureFolders = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "Textures")).GetDirectories("*", SearchOption.AllDirectories);
                    if (textureFolders.Count() == 0)
                    {
                        Monitor.Log($"No sub-folders found under Textures for the content pack {contentPack.Manifest.Name}!", LogLevel.Warn);
                        continue;
                    }

                    // Load in the alternative textures
                    foreach (var textureFolder in textureFolders)
                    {
                        if (!File.Exists(Path.Combine(textureFolder.FullName, "texture.json")))
                        {
                            if (textureFolder.GetDirectories().Count() == 0)
                            {
                                Monitor.Log($"Content pack {contentPack.Manifest.Name} is missing a texture.json under {textureFolder.Name}!", LogLevel.Warn);
                            }

                            continue;
                        }

                        var parentFolderName = textureFolder.Parent.FullName.Replace(contentPack.DirectoryPath + Path.DirectorySeparatorChar, String.Empty);
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
                        string originalItemName = baseModel.ItemName;
                        if (baseModel.HandleNameChanges() is List<string> changedNames && changedNames.Count > 0)
                        {
                            foreach (var changedName in changedNames)
                            {
                                Monitor.Log($"The texture {baseModel.ItemName} from {contentPack.Manifest.Name} has an outdated ItemName that was handled automatically: {originalItemName} -> {changedName}", LogLevel.Trace);
                            }
                        }

                        var originalType = baseModel.Type;
                        if (baseModel.HandleTypeChanges())
                        {
                            Monitor.Log($"The texture {baseModel.ItemName} from {contentPack.Manifest.Name} has an outdated Type that was handled automatically: {originalType} -> {baseModel.Type}", LogLevel.Trace);
                        }

                        // Combine the two collective lists
                        var collectedCollective = new List<dynamic>();
                        foreach (string itemName in baseModel.CollectiveNames)
                        {
                            collectedCollective.Add(new { Name = itemName, IsId = false });
                        }
                        foreach (string itemId in baseModel.CollectiveIds)
                        {
                            collectedCollective.Add(new { Name = itemId, IsId = true });
                        }

                        // Attempt to add an instance of each season
                        var seasons = baseModel.Seasons;
                        for (int s = 0; s < 4; s++)
                        {
                            if ((seasons.Count() == 0 && s > 0) || (seasons.Count() > 0 && s >= seasons.Count()))
                            {
                                continue;
                            }

                            // Attempt to add each instance under CollectiveNames
                            foreach (var textureData in collectedCollective)
                            {
                                // Parse the model and assign it the content pack's owner
                                AlternativeTextureModel textureModel = baseModel.ShallowCopy();

                                // Set the ItemName or ItemId depending on IsId flag
                                if (textureData.IsId is true)
                                {
                                    textureModel.ItemId = textureData.Name;
                                }
                                else
                                {
                                    // Override Grass Alternative Texture pack ItemName to always be Grass, in order to be compatible with translations 
                                    textureModel.ItemName = textureModel.Type.ToString() == "Grass" ? "Grass" : textureData.Name;
                                }

                                // Verify that ItemName or ItemNames is given
                                if (collectedCollective.Count() == 0)
                                {
                                    Monitor.Log($"Unable to add alternative texture for {textureModel.Owner}: Missing the ItemName, ItemId, CollectiveNames or CollectiveIds property! See the log for additional details.", LogLevel.Warn);
                                    Monitor.Log($"Unable to add alternative texture for {textureModel.Owner}: Missing the ItemName, ItemId, CollectiveNames or CollectiveIds property found in the following path: {textureFolder.FullName}", LogLevel.Trace);
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
                                textureModel.ModelName = String.IsNullOrEmpty(textureModel.Season) ? String.Concat(textureModel.GetTextureType(), "_", textureModel.ItemName) : String.Concat(textureModel.GetTextureType(), "_", textureModel.ItemName, "_", textureModel.Season);
                                textureModel.TextureId = String.Concat(textureModel.Owner, ".", textureModel.ModelName);

                                // Verify we are given a texture and if so, track it
                                if (!File.Exists(Path.Combine(textureFolder.FullName, "texture.png")))
                                {
                                    // No texture.png found, may be using split texture files (texture_1.png, texture_2.png, etc.)
                                    var textureFilePaths = Directory.GetFiles(textureFolder.FullName, "texture_*.png")
                                        .Select(t => Path.GetFileName(t))
                                        .Where(t => t.Any(char.IsDigit))
                                        .OrderBy(t => Int32.Parse(Regex.Match(t, @"\d+").Value));

                                    if (textureFilePaths.Count() == 0)
                                    {
                                        Monitor.Log($"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: No associated texture.png or split textures (texture_1.png, texture_2.png, etc.) given. See the log for additional details.", LogLevel.Warn);
                                        Monitor.Log($"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: No associated texture.png or split textures (texture_1.png, texture_2.png, etc.) found in the following path: {textureFolder.FullName}", LogLevel.Trace);
                                        continue;
                                    }
                                    else if (textureModel.IsDecoration())
                                    {
                                        Monitor.Log($"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: Split textures (texture_1.png, texture_2.png, etc.) are not allowed for Decoration types (wallpapers / floors). See the log for additional details.", LogLevel.Warn);
                                        Monitor.Log($"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: Split textures (texture_1.png, texture_2.png, etc.) are not allowed for Decoration types (wallpapers / floors). Located in the following path: {textureFolder.FullName}", LogLevel.Trace);
                                        continue;
                                    }

                                    if (textureModel.GetVariations() < textureFilePaths.Count())
                                    {
                                        Monitor.Log($"Warning for alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: There are less variations specified in texture.json than split textures files. See the log for additional details.", LogLevel.Warn);
                                        Monitor.Log($"Warning for alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: There are less variations specified in texture.json than split textures files found in the following path: {textureFolder.FullName}", LogLevel.Trace);
                                    }
                                    else if (textureModel.IsManualVariationsValid() is false)
                                    {
                                        Monitor.Log($"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: ManualVariations is used but does not start with ID == 0 (the propery should be zero-indexed). See the log for additional details.", LogLevel.Warn);
                                        Monitor.Log($"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: ManualVariations is used but does not start with ID == 0 (the propery should be zero-indexed). Adjust the ID order so that it starts with ID = 0. Located in the following path: {textureFolder.FullName}", LogLevel.Trace);
                                        continue;
                                    }

                                    // Load in the first texture_#.png to get its dimensions for creating stitchedTexture
                                    if (!StitchTexturesToModel(textureModel, contentPack, Path.Combine(parentFolderName, textureFolder.Name), textureFilePaths.Take(textureModel.GetVariations())))
                                    {
                                        continue;
                                    }

                                    textureModel.TileSheetPath = contentPack.ModContent.GetInternalAssetName(Path.Combine(parentFolderName, textureFolder.Name, textureFilePaths.First())).Name;
                                }
                                else
                                {
                                    // Load in the single vertical texture
                                    textureModel.TileSheetPath = contentPack.ModContent.GetInternalAssetName(Path.Combine(parentFolderName, textureFolder.Name, "texture.png")).Name;
                                    Texture2D singularTexture = contentPack.ModContent.Load<Texture2D>(textureModel.TileSheetPath);
                                    if (singularTexture.Height >= AlternativeTextureModel.MAX_TEXTURE_HEIGHT)
                                    {
                                        Monitor.Log($"Unable to add alternative texture for {textureModel.Owner}: The texture {textureModel.TextureId} has a height larger than 16384!\nPlease split it into individual textures (e.g. texture_0.png, texture_1.png, etc.) to resolve this issue. See the log for additional details.", LogLevel.Warn);
                                        Monitor.Log($"Unable to add alternative texture for {textureModel.Owner}: The texture {textureModel.TextureId} has a height larger than 16384!\nPlease split it into individual textures (e.g. texture_0.png, texture_1.png, etc.) to resolve this issue. Located in the following path: {textureFolder.FullName}", LogLevel.Trace);
                                        continue;
                                    }
                                    else if (textureModel.IsDecoration())
                                    {
                                        if (singularTexture.Width < 256)
                                        {
                                            Monitor.Log($"Unable to add alternative texture for {textureModel.ItemName} from {contentPack.Manifest.Name}: The required image width is 256 for Decoration types (wallpapers / floors). Please correct the image's width manually. See the log for additional details.", LogLevel.Warn);
                                            Monitor.Log($"Unable to add alternative texture for {textureModel.ItemName} from {contentPack.Manifest.Name}: The required image width is 256 for Decoration types (wallpapers / floors). Please correct the image's width manually at the following path: {textureFolder.FullName}", LogLevel.Trace);
                                            continue;
                                        }

                                        textureModel.Textures[0] = singularTexture;
                                    }
                                    else if (!SplitVerticalTexturesToModel(textureModel, contentPack.Manifest.Name, singularTexture))
                                    {
                                        continue;
                                    }
                                }

                                // Track the texture model
                                textureManager.AddAlternativeTexture(textureModel);

                                // Log it
                                if (modConfig.OutputTextureDataToLog)
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
                monitor.Log($"[{contentPack.Manifest.Name}] finished loading in {Math.Round(individualLoadingStopwatch.ElapsedMilliseconds / 1000f, 2)} seconds", LogLevel.Trace);
            }

            // Clear the wallpaper / flooring cache
            Helper.GameContent.InvalidateCache("Data/AdditionalWallpaperFlooring");

            collectiveLoadingStopwatch.Stop();
            monitor.Log($"Finished loading all content packs in {Math.Round(collectiveLoadingStopwatch.ElapsedMilliseconds / 1000f, 2)} seconds", LogLevel.Trace);
        }

        internal bool SplitVerticalTexturesToModel(AlternativeTextureModel textureModel, string contentPackName, Texture2D verticalTexture)
        {
            try
            {
                for (int v = 0; v < textureModel.GetVariations(); v++)
                {
                    var extractRectangle = new Rectangle(0, textureModel.TextureHeight * v, verticalTexture.Width, textureModel.TextureHeight);
                    Color[] extractPixels = new Color[extractRectangle.Width * extractRectangle.Height];

                    if (verticalTexture.Bounds.Contains(extractRectangle) is false)
                    {
                        int maxVariationsPossible = verticalTexture.Height / textureModel.TextureHeight;

                        Monitor.Log($"Unable to add alternative texture for item {textureModel.ItemName} from {contentPackName}: More variations specified ({textureModel.GetVariations()}) than given ({maxVariationsPossible})", LogLevel.Warn);
                        return false;
                    }

                    // Get the required pixels
                    verticalTexture.GetData(0, extractRectangle, extractPixels, 0, extractPixels.Length);

                    // Set the required pixels
                    var extractedTexture = new Texture2D(Game1.graphics.GraphicsDevice, extractRectangle.Width, extractRectangle.Height);
                    extractedTexture.SetData(extractPixels);

                    textureModel.Textures[v] = (extractedTexture);
                }
            }
            catch (Exception exception)
            {
                Monitor.Log($"Unable to add alternative texture for item {textureModel.ItemName} from {contentPackName}: Unhandled framework error: {exception}", LogLevel.Warn);
                return false;
            }

            return true;
        }

        private bool StitchTexturesToModel(AlternativeTextureModel textureModel, IContentPack contentPack, string rootPath, IEnumerable<string> textureFilePaths)
        {
            Texture2D baseTexture = contentPack.ModContent.Load<Texture2D>(Path.Combine(rootPath, textureFilePaths.First()));

            // If there is only one split texture file, skip the rest of the logic to avoid issues
            if (textureFilePaths.Count() == 1 || textureModel.GetVariations() == 1)
            {
                if (textureModel.GetVariations() == 1 && textureFilePaths.Count() > 1)
                {
                    Monitor.Log($"Detected more split textures ({textureFilePaths.Count()}) than specified variations ({textureModel.GetVariations()}) for {textureModel.TextureId} from {contentPack.Manifest.Name}", LogLevel.Warn);
                }

                textureModel.Textures[0] = baseTexture;
                return true;
            }

            try
            {
                int variation = 0;
                foreach (var textureFilePath in textureFilePaths)
                {
                    var splitTexture = contentPack.ModContent.Load<Texture2D>(Path.Combine(rootPath, textureFilePath));
                    textureModel.Textures[variation] = splitTexture;

                    variation++;
                }
            }
            catch (Exception exception)
            {
                Monitor.Log($"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: Unhandled framework error: {exception}", LogLevel.Warn);
                return false;
            }

            return true;
        }

        private void DebugSpawnMonsters(string command, string[] args)
        {
            if (args.Length == 0)
            {
                Monitor.Log($"Missing required arguments: [MONSTER_ID]", LogLevel.Warn);
                return;
            }

            int amountToSpawn = 1;
            if (args.Length > 1 && Int32.TryParse(args[1], out amountToSpawn) is false)
            {
                Monitor.Log($"Invalid count given for (QUANTITY)", LogLevel.Warn);
                return;
            }
            Type monsterType = Type.GetType("StardewValley.Monsters." + args[0] + ",Stardew Valley");

            Monitor.Log(Game1.player.Tile.ToString(), LogLevel.Debug);
            for (int i = 0; i < amountToSpawn; i++)
            {
                var monster = Activator.CreateInstance(monsterType, new object[] { Game1.player.Tile }) as Monster;
                monster.Position = Game1.player.Position;
                Game1.currentLocation.characters.Add(monster);
            }
        }

        private void DebugSpawnGiantCrop(string command, string[] args)
        {
            if (args.Length == 0)
            {
                Monitor.Log($"Missing required arguments: [HARVEST_ID]", LogLevel.Warn);
                return;
            }

            if (!(Game1.currentLocation.GetData()?.CanPlantHere ?? Game1.currentLocation.IsFarm) || (Game1.currentLocation is not Farm && !Game1.currentLocation.HasMapPropertyWithValue("AllowGiantCrops")))
            {
                Monitor.Log($"Command can only be used on a plantable location allowing giant crops.", LogLevel.Warn);
                return;
            }

            GameLocation gameLocation = Game1.currentLocation;

            foreach (var tile in gameLocation.terrainFeatures.Pairs.Where(t => t.Value is HoeDirt))
            {
                Crop crop = (tile.Value as HoeDirt).crop;

                if (crop is null || crop.indexOfHarvest.Value != args[0])
                {
                    continue;
                }

                if (crop.TryGetGiantCrops(out var giantCrops))
                {
                    Vector2 vector = crop.tilePosition;
                    Point point = Utility.Vector2ToPoint(vector);

                    foreach (KeyValuePair<string, GiantCropData> item in giantCrops)
                    {
                        string key = item.Key;
                        GiantCropData value = item.Value;
                        bool flag = true;

                        for (int i = point.Y; i < point.Y + value.TileSize.Y; i++)
                        {
                            for (int j = point.X; j < point.X + value.TileSize.X; j++)
                            {
                                Vector2 key2 = new(j, i);

                                if (!gameLocation.terrainFeatures.TryGetValue(key2, out TerrainFeature terrainFeature) || terrainFeature is not HoeDirt hoeDirt2 || hoeDirt2.crop?.indexOfHarvest.Value != crop.indexOfHarvest.Value)
                                {
                                    flag = false;
                                    break;
                                }
                            }
                            if (!flag)
                            {
                                break;
                            }
                        }
                        if (!flag)
                        {
                            continue;
                        }
                        for (int k = point.Y; k < point.Y + value.TileSize.Y; k++)
                        {
                            for (int l = point.X; l < point.X + value.TileSize.X; l++)
                            {
                                Vector2 key3 = new(l, k);

                                ((HoeDirt)gameLocation.terrainFeatures[key3]).crop = null;
                            }
                        }
                        gameLocation.resourceClumps.Add(new GiantCrop(key, vector));
                        break;
                    }
                }
            }
        }

        private void DebugSpawnResourceClump(string command, string[] args)
        {
            if (args.Length == 0)
            {
                Monitor.Log($"Missing required arguments: [RESOURCE_NAME]", LogLevel.Warn);
                return;
            }

            if (!Game1.currentLocation.IsOutdoors)
            {
                Monitor.Log($"Command can only be used outdoors.", LogLevel.Warn);
                return;
            }

            if (args[0].ToLower() != "stump")
            {
                Monitor.Log($"That resource isn't supported.", LogLevel.Warn);
                return;
            }

            Game1.currentLocation.resourceClumps.Add(new ResourceClump(600, 2, 2, Game1.player.Tile + new Vector2(1, 1)));
        }

        private void DebugSpawnChild(string command, string[] args)
        {
            if (args.Length < 2)
            {
                Monitor.Log($"Missing required arguments: [AGE] [IS_MALE] [SKIN_TONE]", LogLevel.Warn);
                return;
            }

            var age = -1;
            if (!int.TryParse(args[0], out age) || age < 0)
            {
                Monitor.Log($"Invalid number given: {args[0]}", LogLevel.Warn);
                return;
            }

            var isMale = false;
            if (args[1].ToLower() == "true")
            {
                isMale = true;
            }

            var hasDarkSkin = false;
            if (args[2].ToLower() == "dark")
            {
                hasDarkSkin = true;
            }

            var child = new Child("Test", isMale, hasDarkSkin, Game1.player);
            child.Position = Game1.player.Position;
            child.Age = age;
            Game1.currentLocation.characters.Add(child);
        }

        private void DebugSetAge(string command, string[] args)
        {
            if (args.Length == 0)
            {
                Monitor.Log($"Missing required arguments: [AGE]", LogLevel.Warn);
                return;
            }

            var age = -1;
            if (!int.TryParse(args[0], out age))
            {
                Monitor.Log($"Invalid number given: {args[0]}", LogLevel.Warn);
                return;
            }

            foreach (var child in Game1.currentLocation.characters.Where(c => c is Child))
            {
                child.Age = 3;
            }
        }

        private void DebugShowPaintShop(string command, string[] args)
        {
            var items = new Dictionary<ISalable, ItemStockInformation>()
            {
                { PatchTemplate.GetPaintBucketTool(), new ItemStockInformation(500, 1) },
                { PatchTemplate.GetScissorsTool(), new ItemStockInformation(500, 1) },
                { PatchTemplate.GetPaintBrushTool(), new ItemStockInformation(500, 1) },
                { PatchTemplate.GetSprayCanTool(true), new ItemStockInformation(500, 1) },
                { PatchTemplate.GetCatalogueTool(), new ItemStockInformation(500, 1) }
            };
            Game1.activeClickableMenu = new ShopMenu("Alternative Textures Debug", items);
        }

        private void DebugSetTexture(string command, string[] args)
        {
            if (args.Length == 0)
            {
                Monitor.Log($"Missing required arguments: [TEXTURE_ID]", LogLevel.Warn);
                return;
            }

            string? season = null;
            if (args.Length > 1)
            {
                season = args[1];
            }

            int variation = 0;
            if (args.Length > 2 && Int32.TryParse(args[2], out int parsedVariation))
            {
                variation = parsedVariation;
            }

            var objectBelowPlayer = PatchTemplate.GetObjectAt(Game1.currentLocation, (int)(Game1.player.Tile.X * 64), (int)(Game1.player.Tile.Y + 1) * 64);
            if (objectBelowPlayer is null)
            {
                Monitor.Log($"No object detected below the player!", LogLevel.Warn);
                return;
            }
            monitor.Log($"Attempting to change texture of {objectBelowPlayer.Name} to {args[0]}", LogLevel.Debug);

            _api.SetTextureForObject(objectBelowPlayer, args[0], season, variation);
        }

        private void DebugClearTexture(string command, string[] args)
        {
            var objectBelowPlayer = PatchTemplate.GetObjectAt(Game1.currentLocation, (int)(Game1.player.Tile.X * 64), (int)(Game1.player.Tile.Y + 1) * 64);
            if (objectBelowPlayer is null)
            {
                Monitor.Log($"No object detected below the player!", LogLevel.Warn);
                return;
            }
            monitor.Log($"Clearing the texture of {objectBelowPlayer.Name}", LogLevel.Debug);

            _api.ClearTextureForObject(objectBelowPlayer);
        }

        private string CleanContentPackNameForConfig(string contentPackName)
        {
            return contentPackName.Replace("[", String.Empty).Replace("]", String.Empty);
        }
    }
}
