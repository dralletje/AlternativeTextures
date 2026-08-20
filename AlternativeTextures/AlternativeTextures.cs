global using AlternativeTextures.Framework.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using AlternativeTextures.Framework.External.GenericModConfigMenu;
using AlternativeTextures.Framework.Managers;
using AlternativeTextures.Framework.Models;
using AlternativeTextures.Framework.Patches;
using AlternativeTextures.Framework.Patches.Buildings;
using AlternativeTextures.Framework.Patches.Entities;
using AlternativeTextures.Framework.Patches.GameLocations;
using AlternativeTextures.Framework.Patches.SpecialObjects;
using AlternativeTextures.Framework.Patches.StandardObjects;
using AlternativeTextures.Framework.Patches.Tools;
using AlternativeTextures.Framework.Utilities.Extensions;
using AlternativeTextures.Tools;
using ConsoleLog;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData;
using StardewValley.Menus;
using StardewValley.Tools;

namespace AlternativeTextures;

public enum TextureType
{
    Unknown,
    Craftable,
    Grass,
    Tree,
    FruitTree,
    Crop,
    GiantCrop,
    ResourceClump,
    Bush,
    Flooring,
    Furniture,
    Character,
    Building,
    Decoration,
    ArtifactSpot,
}

public static class TextureTypeExtensions
{
    public static ModelIdentifier WithName(this TextureType type, string name) =>
        new()
        {
            Type = type,
            IsName = true,
            String = name,
        };
}

static class Monitor
{
    public static IMonitor? monitor;

    public static void Log(string message, LogLevel loglevel = LogLevel.Trace)
    {
        if (monitor is { } actual)
        {
            actual.Log(message, loglevel);
        }
        else
        {
            Console.WriteLine(message);
        }
    }

    public static void LogOnce(string message, LogLevel loglevel = LogLevel.Trace)
    {
        if (monitor is { } actual)
        {
            actual.LogOnce(message, loglevel);
        }
        else
        {
            Console.WriteLine(message);
        }
    }
}

public class AlternativeTextures : Mod
{
    internal const string PAINTPAIL = "(F)PeacefulEnd.AlternativeTexturesContentPatcher_PaintPail";
    internal const string PAINT_BRUSH_EMPTY_ID = "(T)PeacefulEnd.AlternativeTextures_PaintBrush";
    internal const string PAINT_BRUSH_FILLED_ID = "(T)PeacefulEnd.AlternativeTextures_PaintBrushFilled";

    internal const string TOOL_ID_PAINT_BUCKET = $"(T)PeacefulEnd.AlternativeTextures_PaintBucket";
    internal const string TOOL_ID_SCISSORS = $"(T)PeacefulEnd.AlternativeTextures_Scissors";
    internal const string TOOL_ID_SPRAY_CAN = $"(T)PeacefulEnd.AlternativeTextures_SprayCan";
    internal const string TOOL_ID_CATALOGUE = $"(T)PeacefulEnd.AlternativeTextures_Catalogue";

    // Core modData keys
    internal const string TEXTURE_TOKEN_HEADER = "AlternativeTextures/Textures/";
    internal const string DEFAULT_OWNER = "Stardew.Default";
    internal const string ENABLED_SPRAY_CAN_TEXTURES = "Stardew.Default";

    // Tool related keys
    internal const string SPRAY_CAN_FLAG = "AlternativeTextures.SprayCanFlag";
    internal const string SPRAY_CAN_RADIUS = "AlternativeTextures.SprayCanRadius";

    // Mod ID const
    internal const string MOD_ID = "PeacefulEnd.AlternativeTextures";

    // Shared static helpers
    internal static IMonitor monitor;
    internal static IModHelper modHelper;
    internal static Multiplayer multiplayer;
    internal static IManifest modManifest;

    static ModConfigHolder? modConfigHolder;
    internal static ModConfig modConfig
    {
        get { return modConfigHolder?.ModConfig ?? new ModConfig(); }
        set { modConfigHolder?.ModConfig = value; }
    }

    internal ContentPackLoader? contentPackLoader;

    // Managers
    internal static TextureManager textureManager;
    internal static MessageManager messageManager;
    internal static ApiManager apiManager;

    private CustomToolPlugin? customToolPlugin;

    public override void Entry(IModHelper helper)
    {
        // Set up the monitor, helper and multiplayer
        monitor = Monitor;

        global::AlternativeTextures.Monitor.monitor = Monitor;

        modHelper = helper;
        modManifest = ModManifest;
        multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

        modConfigHolder = new ModConfigHolder(this);
        contentPackLoader = new ContentPackLoader(this);

        // Setup our managers
        textureManager = new TextureManager(this);
        messageManager = new MessageManager(helper, ModManifest.UniqueID);

        this.customToolPlugin = new CustomToolPlugin(helper);

        this.customToolPlugin.Start();

        new Commands(this).Register();

        // Load our Harmony patches
        try
        {
            var harmony = new Harmony(this.ModManifest.UniqueID);

            // Apply texture override related patches
            new GameLocationPatch(monitor, helper).Apply(harmony);
            new ObjectPatch(helper).Apply(harmony);
            new FencePatch(helper).Apply(harmony);
            new HoeDirtPatch(monitor, helper).Apply(harmony);
            new CropPatch(helper).Apply(harmony);
            new GiantCropPatch(helper).Apply(harmony);
            new GrassPatch(monitor, helper).Apply(harmony);
            new TreePatch(monitor, helper).Apply(harmony);
            new FruitTreePatch(helper).Apply(harmony);
            new ResourceClumpPatch(monitor, helper).Apply(harmony);
            new BushPatch(monitor, helper).Apply(harmony);
            new FlooringPatch(monitor, helper).Apply(harmony);
            new FurniturePatch(helper).Apply(harmony);
            new BedFurniturePatch(helper).Apply(harmony);
            new FishTankFurniturePatch(helper).Apply(harmony);

            // Start of special objects
            new ChestPatch(monitor, helper).Apply(harmony);
            new CrabPotPatch(monitor, helper).Apply(harmony);
            new IndoorPotPatch(monitor, helper).Apply(harmony);
            new PhonePatch(monitor, helper).Apply(harmony);
            new TorchPatch(monitor, helper).Apply(harmony);
            new WoodChipperPatch(monitor, helper).Apply(harmony);

            // Start of entity patches
            new CharacterPatch(monitor, helper).Apply(harmony);
            new FarmAnimalPatch(monitor, helper).Apply(harmony);
            new HorsePatch(helper).Apply(harmony);
            new PetPatch(monitor, helper).Apply(harmony);
            new MonsterPatch(monitor, helper).Apply(harmony);

            // Start of building patches
            new BuildingPatch(monitor, helper).Apply(harmony);
            new ShippingBinPatch(monitor, helper).Apply(harmony);

            // Start of location patches
            new GameLocationPatch(monitor, helper).Apply(harmony);

            // Paint tool related patches
            new ToolPatch(helper).Apply(harmony);
        }
        catch (Exception e)
        {
            Monitor.Log($"Issue with Harmony patching: {e}", LogLevel.Error);
            return;
        }

        // Hook into GameLoop events
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        // Hook into Input events
        helper.Events.Input.ButtonsChanged += OnButtonChanged;

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
                var clonedTexture = originalTexture.CreateSelectiveCopy(
                    Game1.graphics.GraphicsDevice,
                    new Rectangle(0, 0, originalTexture.Width, originalTexture.Height)
                );
                e.LoadFrom(() => clonedTexture, AssetLoadPriority.Exclusive);
            }
        }
        else if (
            e.NameWithoutLocale.IsEquivalentTo("Data/AdditionalWallpaperFlooring")
            && textureManager.GetValidTextureNamesWithSeason().Count > 0
        )
        {
            e.Edit(asset =>
            {
                var moddedDecorations = asset.GetData<List<ModWallpaperOrFlooring>>();

                foreach (
                    var textureModel in textureManager
                        .GetAllTextures()
                        .Where(t =>
                            t.ForModel.Type is TextureType.Decoration && !moddedDecorations.Any(d => d.Id == t.GetId())
                        )
                )
                {
                    var decoration = new ModWallpaperOrFlooring()
                    {
                        Id = textureModel.GetId(),
                        Texture = $"{AlternativeTextures.TEXTURE_TOKEN_HEADER}{textureModel.GetTokenId()}",
                        IsFlooring = String.Equals(textureModel.ItemName, "Floor", StringComparison.OrdinalIgnoreCase),
                        Count = textureModel.GetVariations(),
                    };

                    moddedDecorations.Add(decoration);
                }
            });
        }
    }

    private void OnButtonChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (
            Game1.activeClickableMenu is null
            && Game1.player.CurrentTool is GenericTool tool
            && e.Held.Contains(SButton.MouseLeft)
        )
        {
            if (tool.QualifiedItemId == TOOL_ID_CATALOGUE)
            {
                ToolPatch.UseTextureCatalogue(Game1.player);
            }
        }
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

        // Load any owned content packs
        contentPackLoader!.Load();

        Monitor.Log($"Finished loading Alternative Textures content packs", LogLevel.Debug);

        // Hook into GMCM, if applicable
        modConfigHolder!.RegisterWithGMC();
    }
}
