global using AlternativeTextures.Framework.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using AlternativeTextures.App.Tools;
using AlternativeTextures.Framework;
using AlternativeTextures.Framework.External.GenericModConfigMenu;
using AlternativeTextures.Framework.Managers;
using AlternativeTextures.MetaFramework;
using AlternativeTextures.PatchDrawMod;
using AlternativeTextures.PatchDrawMod.Patches.Tools;
using ConsoleLog;
using Incubator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData;
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

    internal static IModHelper modHelper;

    // internal static Multiplayer multiplayer;
    internal static IManifest modManifest;

    internal static ModConfig modConfig = new ModConfig();

    internal static TextureManager textureManager;

    AlternativeTexturesDralMod? mod = null;

    public override void Entry(IModHelper helper)
    {
        global::AlternativeTextures.Monitor.monitor = Monitor;

        textureManager = new(helper);

        modHelper = helper;
        modManifest = ModManifest;
        // multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

        mod = new AlternativeTexturesDralMod(
            new DralModContext<ValueTuple, AlternativeTexturesDralMod>()
            {
                Helper = Helper,
                ModManifest = ModManifest,
                Monitor = Monitor,
                GetParent = () => new(),
                GetMe = () => mod ?? throw new ArgumentException("GetMe called before initialization"),
            }
        );

        mod.Entry();
    }
}

class AlternativeTexturesDralMod(DralModContext<ValueTuple, AlternativeTexturesDralMod> context)
    : DralMod,
        HasMod<TextureManager>
{
    IModHelper Helper = context.Helper;
    IMonitor Monitor = context.Monitor;
    IManifest ModManifest = context.ModManifest;

    TextureManager textureManager => AlternativeTextures.textureManager;

    TextureManager HasMod<TextureManager>.GetMod() => AlternativeTextures.textureManager;

    // Shared static helpers

    static ModConfigHolder? modConfigHolder;
    internal static ModConfig modConfig
    {
        get { return modConfigHolder?.ModConfig ?? new ModConfig(); }
        set { modConfigHolder?.ModConfig = value; }
    }

    internal ContentPackLoaderMod.ContentPackLoaderMod<AlternativeTexturesDralMod> contentPackLoaderMod =
        context.Scoped<ContentPackLoaderMod.ContentPackLoaderMod<AlternativeTexturesDralMod>>(
            (context) => new(context)
        );

    // internal PatchDrawMod<AlternativeTexturesDralMod> patchDrawMod = context.Scoped<
    //     PatchDrawMod<AlternativeTexturesDralMod>
    // >((context) => new(context));

    internal PatchDrawMod<AlternativeTexturesDralMod> patchDrawMod = context.Scoped<
        PatchDrawMod<AlternativeTexturesDralMod>
    >((context) => new(context));

    // Managers
    internal static ApiManager apiManager;

    private CustomToolPlugin? customToolPlugin;

    public override IDisposable? Entry()
    {
        // Set up the monitor, helper and multiplayer

        // multiplayer.broadcastSprites;

        Console.Log(
            $"Helper.ModRegistry.IsLoaded(spacechase0.MoreGiantCrops): {Helper.ModRegistry.IsLoaded("spacechase0.MoreGiantCrops")}"
        );

        // modConfigHolder = new ModConfigHolder(this);

        // this.customToolPlugin = new CustomToolPlugin(helper);

        // this.customToolPlugin.Start();

        // new Commands(this).Register();

        // // Hook into GameLoop events

        // // Hook into Input events
        // helper.Events.Input.ButtonsChanged += OnButtonChanged;

        // // Hook into the Content events
        Helper.Events.Content.AssetRequested += OnContentAssetRequested;
        // // helper.Events.Content.AssetReady += OnContentAssetReady;

        contentPackLoaderMod.Entry();
        // Load our Harmony patches
        patchDrawMod.Entry();

        return null;
    }

    // private void OnContentAssetReady(object? sender, AssetReadyEventArgs e)
    // {
    //     var asset = e.Name;
    //     if (textureManager.GetTextureByToken(asset.Name) is Texture2D texture)
    //     {
    //         var loadedTexture = Helper.GameContent.Load<Texture2D>(asset.Name);

    //         textureManager.UpdateTexture(asset.Name, loadedTexture);
    //     }
    // }

    private void OnContentAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.DataType == typeof(Texture2D))
        {
            var asset = e.Name;
            if (textureManager.GetModelByToken(asset.Name) is { } textureModel)
            {
                var originalTexture = textureModel.Texture.Texture;
                var clonedTexture = originalTexture.CreateSelectiveCopy(
                    Game1.graphics.GraphicsDevice,
                    new Rectangle(0, 0, originalTexture.Width, originalTexture.Height)
                );
                e.LoadFrom(() => clonedTexture, AssetLoadPriority.Exclusive);
            }
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/AdditionalWallpaperFlooring"))
        {
            e.Edit(asset =>
            {
                var moddedDecorations = asset.GetData<List<ModWallpaperOrFlooring>>();

                foreach (
                    var textureModel in textureManager
                        .GetAllTextures()
                        .Where(t =>
                            t.ForModel.Type is TextureType.Decoration
                            && !moddedDecorations.Any(d =>
                                d.Id == DecorationIdHelper.ToString(t.UniqueIdentifierWithoutSeason)
                            )
                        )
                )
                {
                    var texture =
                        $"{AlternativeTextures.TEXTURE_TOKEN_HEADER}{textureModel.Owner}/{textureModel.ForModel.Type}/{textureModel.ForModel.String}/{textureModel.Variation}";

                    var decoration = new ModWallpaperOrFlooring()
                    {
                        Id = DecorationIdHelper.ToString(textureModel.UniqueIdentifierWithoutSeason),
                        Texture = texture,
                        IsFlooring = textureModel.ForModel == ModelIdentifier.Floor,
                        Count = 1,
                    };

                    moddedDecorations.Add(decoration);
                }
            });
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

        Monitor.Log($"Finished loading Alternative Textures content packs", LogLevel.Debug);

        // Hook into GMCM, if applicable
        // modConfigHolder!.RegisterWithGMC();
    }
}
