using System.Collections.Generic;
using AlternativeTextures.Framework.Patches;
using Incubator;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace AlternativeTextures;

public class Commands(AlternativeTextures mod)
{
    readonly IModHelper Helper = mod.Helper;
    readonly IMonitor Monitor = mod.Monitor;

    public void Register()
    {
        // Set up the monitor, helper and multiplayer
        Helper.ConsoleCommands.Add(
            "at_paint_shop",
            "Shows the carpenter shop with the paint bucket for sale.\n\nUsage: at_paint_shop",
            callback: (command, args) =>
            {
                var items = new Dictionary<ISalable, ItemStockInformation>()
                {
                    {
                        ItemRegistry.Create<GenericTool>(AlternativeTextures.TOOL_ID_PAINT_BUCKET),
                        new ItemStockInformation(0, 1)
                    },
                    {
                        ItemRegistry.Create<GenericTool>(AlternativeTextures.TOOL_ID_SCISSORS),
                        new ItemStockInformation(0, 1)
                    },
                    {
                        ItemRegistry.Create<GenericTool>(AlternativeTextures.PAINT_BRUSH_EMPTY_ID),
                        new ItemStockInformation(0, 1)
                    },
                    {
                        ItemRegistry.Create<GenericTool>(AlternativeTextures.TOOL_ID_SPRAY_CAN),
                        new ItemStockInformation(0, 1)
                    },
                    {
                        ItemRegistry.Create<GenericTool>(AlternativeTextures.TOOL_ID_CATALOGUE),
                        new ItemStockInformation(0, 1)
                    },
                    { ItemRegistry.Create<GenericTool>(AlternativeTextures.PAINTPAIL), new ItemStockInformation(0, 1) },
                };
                Game1.activeClickableMenu = new ShopMenu("Alternative Textures Debug", items);
            }
        );
        // Helper.ConsoleCommands.Add(
        //     "at_set_object_texture",
        //     "Sets the texture of the object below the player.\n\nUsage: at_set_object_texture [TEXTURE_ID] (VARIATION_NUMBER) (SEASON)",
        //     callback: (command, args) =>
        //     {
        //         var objectBelowPlayer = PatchTemplate.GetObjectAt(
        //             Game1.currentLocation,
        //             (int)(Game1.player.Tile.X * 64),
        //             (int)(Game1.player.Tile.Y + 1) * 64
        //         );
        //         if (objectBelowPlayer is null)
        //         {
        //             Monitor.Log($"No object detected below the player!", LogLevel.Warn);
        //             return;
        //         }

        //         // (var textureId, var season, var variant) = args switch
        //         // {
        //         //     [] => throw new CommandException(),
        //         //     [var _textureId] => (_textureId, (string?)null, 0),
        //         //     [var _textureId, var variantString] when variantString.asInt() is int _variant => (
        //         //         _textureId,
        //         //         (string?)null,
        //         //         _variant
        //         //     ),
        //         //     [var _textureId, var _season] => (_textureId, _season, 0),
        //         //     [var _textureId, var _season, var variantString] when variantString.asInt() is int _variant => (
        //         //         _textureId,
        //         //         _season,
        //         //         _variant
        //         //     ),
        //         // };

        //         switch (args)
        //         {
        //             case []:
        //                 Monitor.Log($"Missing required arguments: [TEXTURE_ID]", LogLevel.Warn);
        //                 return;

        //             case [var textureId]:
        //                 Monitor.Log(
        //                     $"Attempting to change texture of {objectBelowPlayer.Name} to {textureId}",
        //                     LogLevel.Debug
        //                 );
        //                 AlternativeTextures._api.SetTextureForObject(objectBelowPlayer, textureId, null, 0);
        //                 break;

        //             case [var textureId, var variantString] when variantString.asInt() is int variant:
        //                 Monitor.Log(
        //                     $"Attempting to change texture of {objectBelowPlayer.Name} to {textureId}:{variant}",
        //                     LogLevel.Debug
        //                 );
        //                 AlternativeTextures._api.SetTextureForObject(objectBelowPlayer, textureId, null, variant);
        //                 break;

        //             case [var textureId, var season]:
        //                 Monitor.Log(
        //                     $"Attempting to change texture of {objectBelowPlayer.Name} to {textureId} during {season}",
        //                     LogLevel.Debug
        //                 );
        //                 AlternativeTextures._api.SetTextureForObject(objectBelowPlayer, textureId, season, 0);
        //                 break;

        //             case [var textureId, var season, var variantString] when variantString.asInt() is int variant:
        //                 Monitor.Log(
        //                     $"Attempting to change texture of {objectBelowPlayer.Name} to {textureId}:{variant} during {season}",
        //                     LogLevel.Debug
        //                 );
        //                 AlternativeTextures._api.SetTextureForObject(objectBelowPlayer, textureId, season, variant);
        //                 break;
        //         }
        //     }
        // );
        // Helper.ConsoleCommands.Add(
        //     "at_clear_texture",
        //     "Clears the texture of the object below the player.\n\nUsage: at_clear_texture",
        //     callback: (command, args) =>
        //     {
        //         var objectBelowPlayer = PatchTemplate.GetObjectAt(
        //             Game1.currentLocation,
        //             (int)(Game1.player.Tile.X * 64),
        //             (int)(Game1.player.Tile.Y + 1) * 64
        //         );
        //         if (objectBelowPlayer is null)
        //         {
        //             Monitor.Log($"No object detected below the player!", LogLevel.Warn);
        //             return;
        //         }
        //         Monitor.Log($"Clearing the texture of {objectBelowPlayer.Name}", LogLevel.Debug);
        //         AlternativeTextures._api.ClearTextureForObject(objectBelowPlayer);
        //     }
        // );
        // Helper.ConsoleCommands.Add(
        //     "at_reload",
        //     "Reloads all Alternative Texture content packs.\n\nUsage: at_reload",
        //     delegate
        //     {
        //         mod.contentPackLoader.Load();
        //     }
        // );
    }
}
