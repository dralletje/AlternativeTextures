global using AlternativeTextures.Framework.Models;
global using AlternativeTextures.Stardew;
global using ConsoleLog;
using System;
using System.Collections.Generic;
using System.Linq;
using AlternativeTextures.App;
using AlternativeTextures.CustomToolMod;
using AlternativeTextures.Framework;
using AlternativeTextures.Framework.Managers;
using AlternativeTextures.Framework.Paintable;
using AlternativeTextures.MetaFramework;
using AlternativeTextures.PatchDrawMod;
using Incubator;
using Incubator.MonoGame;
using Incubator.MonoGame.FlexibleTextures;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData;

namespace AlternativeTextures;

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

static class ModHelper
{
    public static IModHelper shared;
}

public static class Game2
{
    public static readonly State<bool> SnappyMenus = new(Game1.options.SnappyMenus);
}

class ModConfigStub
{
    public bool IsTextureVariationDisabled(string identifier, int variant) => false;
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

    internal static readonly ModConfigStub modConfig = new();

    // internal static Multiplayer multiplayer;
    internal static TextureManager textureManager;

    AlternativeTexturesDralMod? mod = null;

    public override void Entry(IModHelper helper)
    {
        global::AlternativeTextures.Monitor.monitor = Monitor;
        global::AlternativeTextures.ModHelper.shared = helper;

        textureManager = new(helper);

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
        HasMod<TextureManager>,
        HasMod<ICustomToolMod>
{
    IModHelper Helper = context.Helper;
    IMonitor Monitor = context.Monitor;
    IManifest ModManifest = context.ModManifest;

    TextureManager textureManager => AlternativeTextures.textureManager;

    TextureManager HasMod<TextureManager>.GetMod() => AlternativeTextures.textureManager;

    internal ContentPackLoaderMod.ContentPackLoaderMod<AlternativeTexturesDralMod> contentPackLoaderMod =
        context.Scoped<ContentPackLoaderMod.ContentPackLoaderMod<AlternativeTexturesDralMod>>(
            (context) => new(context)
        );

    internal PatchDrawMod<AlternativeTexturesDralMod> patchDrawMod = context.Scoped<
        PatchDrawMod<AlternativeTexturesDralMod>
    >((context) => new(context));

    internal CustomToolMod<AlternativeTexturesDralMod> CustomToolPlugin = context.Scoped<
        CustomToolMod<AlternativeTexturesDralMod>
    >((context) => new(context));

    ICustomToolMod HasMod<ICustomToolMod>.GetMod() => CustomToolPlugin;

    internal App<AlternativeTexturesDralMod> App = context.Scoped<App<AlternativeTexturesDralMod>>(
        (context) => new(context)
    );

    public override IDisposable? Entry()
    {
        // Set up the monitor, helper and multiplayer

        // multiplayer.broadcastSprites;

        // modConfigHolder = new ModConfigHolder(this);

        // this.customToolPlugin = new CustomToolPlugin(helper);

        // this.customToolPlugin.Start();

        // new Commands(this).Register();

        Helper.Events.GameLoop.UpdateTicking += (sender, input) =>
        {
            if (Game2.SnappyMenus.Value != Game1.options.SnappyMenus)
            {
                Game2.SnappyMenus.Value = Game1.options.SnappyMenus;
            }
        };

        // // Hook into the Content events
        Helper.Events.Content.AssetRequested += OnContentAssetRequested;
        // // helper.Events.Content.AssetReady += OnContentAssetReady;

        CustomToolPlugin.Entry();
        contentPackLoaderMod.Entry();
        patchDrawMod.Entry();
        App.Entry();

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
            if (textureManager.GetTextureForPath(asset.Name) is { } textureModel)
            {
                var isCurrentlyDrawing = Helper.Reflection.GetField<bool>(Game1.spriteBatch, "_beginCalled").GetValue();
                Console.Log(
                    $"Loading {asset.Name} ({textureModel.Texture.Height} x {textureModel.Texture.Width}) where drawing = {isCurrentlyDrawing}"
                );

                e.LoadFrom(
                    () =>
                    {
                        var isCurrentlyDrawing = Helper
                            .Reflection.GetField<bool>(Game1.spriteBatch, "_beginCalled")
                            .GetValue();
                        Console.Log($"Loaded {asset.Name} while drawing = {isCurrentlyDrawing}");
                        var clonedTexture = isCurrentlyDrawing
                            ? textureModel.CreateTexture()
                            : new IdentityTexture(textureModel.Texture).Flatten(Game1.graphics.GraphicsDevice);
                        return clonedTexture;
                    },
                    AssetLoadPriority.Exclusive
                );

                // e.LoadFrom(() => textureModel.Texture, AssetLoadPriority.Exclusive);
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
                    // var texture =
                    //     $"{AlternativeTextures.TEXTURE_TOKEN_HEADER}{textureModel.Owner}/{textureModel.ForModel.Type}/{textureModel.ForModel.String}/{textureModel.Variation}";
                    var texture = textureModel.TexturePath;
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
        Monitor.Log($"Finished loading Alternative Textures content packs", LogLevel.Debug);
        // Hook into GMCM, if applicable
        // modConfigHolder!.RegisterWithGMC();
    }
}
