using System;
using System.Collections.Generic;
using System.Linq;
using AlternativeTextures.App.UI;
using AlternativeTextures.CustomToolMod;
using AlternativeTextures.Framework;
using AlternativeTextures.Framework.Paintable;
using DralGeometry;
using Incubator;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Tools;

namespace AlternativeTextures.App.Tools;

class PaintBucketTool(IModHelper Helper, GenericTool tool) : ICustomTool
{
    public IDisposable? Start()
    {
        Helper.Events.Input.ButtonPressed += OnButtonPressed;
        return new ActionDisposable(() =>
        {
            Helper.Events.Input.ButtonPressed -= OnButtonPressed;
        });
    }

    public void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (e.Button.IsUseToolButton())
        {
            OnUse(sender, e);
        }
    }

    public void OnUse(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsPlayerFree)
            return;
        if (e.IsMenuPress)
            return;

        var tile = Game1.player.ActiveTargetTile;
        var objects = IPaintable.GetAnythingAtTile(tile).ToList();
        var farmer = Game1.player;
        var location = farmer.currentLocation;

        Helper.Input.Suppress(e.Button);

        /// If you stand before a wall with something on it and you try to use this,
        /// it will only find the wall, not the item.
        /// So a small fix to check on tile above if standing in front of a wall:
        if (objects.Count == 0)
        {
            if (location is DecoratableLocation loc && loc.GetWallpaperID(tile.X, tile.Y) is { } roomId)
            {
                objects = [.. IPaintable.GetAnythingAtTile(tile with { Y = tile.Y - 1 })];
            }
        }

        Console.Log($"objects: {objects.Select(x => x.ToString()).ToList()}");

        foreach (var worldObject in objects)
        {
            if (IPaintable.From(worldObject) is not { } paintable)
                continue;

            Console.Log($"PAINTBUCKET: {paintable.TextureIdentifier}");

            List<TextureInfo> items =
            [
                .. PaintBucketMenuData.VanillaTexturesFor(paintable.ModelIdentifier),
                .. PaintBucketMenuData.GetTexturesFor(paintable.ModelIdentifier),
            ];

            if (items.Count == 1)
            {
                // Game1.addHUDMessage(new HUDMessage(_helper.Translation.Get("messages.warning.no_textures_for_season", new { itemName = modelName }), 3));
                // csharpier-ignore
                Game1.addHUDMessage(new HUDMessage($"No alternative textures found for {paintable.ModelIdentifier.String} ({paintable.ModelIdentifier.Type})"));
                return;
            }

            var gridMenu = new GridMenu(
                [
                    .. items.Select(textureInfo =>
                        (GridMenu.Item)
                            new TextureGridMenuItem()
                            {
                                Related = worldObject,
                                Paintable = paintable,
                                TextureIdentifier = textureInfo.TextureIdentifier,
                                DisplayName = textureInfo.DisplayName,
                            }
                    ),
                ],
                gridSizeFor(paintable, worldObject),
                uiTitle: Helper.Translation.Get("tools.name.paint_bucket"),
                onPress: (item) =>
                {
                    if (item is TextureGridMenuItem betterItem)
                    {
                        betterItem.Paintable.ApplyTexture(betterItem.TextureIdentifier);
                    }
                }
            );

            Game1.activeClickableMenu = gridMenu;
            if (paintable.TextureIdentifier is { } texture)
            {
                gridMenu.ScrollTo(items.FindIndex(x => x.TextureIdentifier == texture));
            }
            return;
        }

        // if (location is DecoratableLocation decoratableLocation)
        // {
        //     if (decoratableLocation.GetWallpaperID(tile.X, tile.Y) is { } wallId)
        //     {
        //         var wallpaperPaintable = new WallpaperDecorationPaintable(decoratableLocation, wallId);
        //         List<TextureInfo> items =
        //         [
        //             .. PaintBucketMenuData.VanillaWallpaperDecorations(),
        //             .. PaintBucketMenuData.GetTexturesFor(wallpaperPaintable.ModelIdentifier),
        //         ];

        //         if (items.Count == 1)
        //         {
        //             // csharpier-ignore
        //             Game1.addHUDMessage(new HUDMessage($"No alternative textures found for {wallpaperPaintable.ModelIdentifier.String} ({wallpaperPaintable.ModelIdentifier.Type})"));
        //         }

        //         var gridMenu = new GridMenu(
        //             [
        //                 .. items.Select(textureInfo => new TextureGridMenuItem()
        //                 {
        //                     Related = null,
        //                     DisplayName = textureInfo.DisplayName,
        //                     TextureIdentifier = textureInfo.TextureIdentifier,
        //                     Paintable = wallpaperPaintable,
        //                 }),
        //             ],
        //             new(rows: 2, columns: 6),
        //             uiTitle: Helper.Translation.Get("tools.name.paint_bucket"),
        //             onPress: (item) =>
        //             {
        //                 if (item is TextureGridMenuItem betterItem)
        //                 {
        //                     betterItem.Paintable.ApplyTexture(betterItem.TextureIdentifier);
        //                 }
        //             }
        //         );

        //         Game1.activeClickableMenu = gridMenu;
        //         if (wallpaperPaintable.TextureIdentifier is { } texture)
        //         {
        //             gridMenu.ScrollTo(items.FindIndex(x => x.TextureIdentifier == texture));
        //         }
        //     }
        //     else if (decoratableLocation.GetFloorID(tile.X, tile.Y) is { } floorId)
        //     {
        //         var floorPaintable = new FloorDecorationPaintable(decoratableLocation, floorId);

        //         List<TextureInfo> items =
        //         [
        //             .. PaintBucketMenuData.VanillaFloorDecorations(),
        //             .. PaintBucketMenuData.GetTexturesFor(floorPaintable.ModelIdentifier),
        //         ];
        //         if (items.Count == 1)
        //         {
        //             // csharpier-ignore
        //             Game1.addHUDMessage(new HUDMessage($"No alternative textures found for {floorPaintable.ModelIdentifier.String} ({floorPaintable.ModelIdentifier.Type})"));
        //         }

        //         var gridMenu = new GridMenu(
        //             [
        //                 .. items.Select(textureInfo => new TextureGridMenuItem()
        //                 {
        //                     Related = null,
        //                     DisplayName = textureInfo.DisplayName,
        //                     TextureIdentifier = textureInfo.TextureIdentifier,
        //                     Paintable = floorPaintable,
        //                 }),
        //             ],
        //             new(rows: 3, columns: 4),
        //             uiTitle: Helper.Translation.Get("tools.name.paint_bucket"),
        //             onPress: (item) =>
        //             {
        //                 if (item is TextureGridMenuItem betterItem)
        //                 {
        //                     betterItem.Paintable.ApplyTexture(betterItem.TextureIdentifier);
        //                 }
        //             }
        //         );

        //         Game1.activeClickableMenu = gridMenu;
        //         if (floorPaintable.TextureIdentifier is { } texture)
        //         {
        //             gridMenu.ScrollTo(items.FindIndex(x => x.TextureIdentifier == texture));
        //         }
        //     }
        // }
    }

    static GridSize gridSizeFor(IPaintable paintable, WorldObject related)
    {
        var _sourceRect = DrawPaintable
            .PreviewTexture(
                (
                    paintable.TextureIdentifier ?? TextureIdentifierWithoutSeason.DefaultFor(paintable.ModelIdentifier)
                ).WithSeason(Game1.currentLocation.GetSeason()),
                related
            )
            ?.SourceRect;
        var sourceRect = _sourceRect ?? new Rectangle(0, 0, 0, 0);

        // var sourceRect = SourceRects.GetSourceRectangle(availableModels.First(), target, availableModels.First().TextureWidth, availableModels.First().TextureHeight, -1);
        return paintable.ModelIdentifier switch
        {
            { Type: TextureType.Craftable } => sourceRect.Height <= 16
                ? new(rows: 4, columns: 6)
                : new(rows: 3, columns: 6),
            { Type: TextureType.Furniture } => sourceRect switch
            {
                { Height: >= 64 } => new(rows: 2, columns: 6),
                { Height: >= 32 } => new(rows: 3, columns: 6),
                _ => new(rows: 4, columns: 6),
            },
            { Type: TextureType.Flooring } => new(rows: 4, columns: 6),
            { Type: TextureType.Character } => new(rows: 4, columns: 6),
            { Type: TextureType.Tree } => new(rows: 2, columns: 6),
            { Type: TextureType.FruitTree } => new(rows: 1, columns: 3),
            { Type: TextureType.Crop } => new(rows: 4, columns: 1),
            { Type: TextureType.GiantCrop } => new(rows: 2, columns: 3),
            { Type: TextureType.Grass } => new(rows: 6, columns: 4),
            { Type: TextureType.Bush } => new(rows: 6, columns: 4),
            { Type: TextureType.Building } => new(rows: 1, columns: 3),
            { Type: TextureType.Decoration, IsName: true, String: "Floor" } => new(rows: 3, columns: 4),
            { Type: TextureType.Decoration, IsName: true, String: "Wallpaper" } => new(rows: 2, columns: 6),
            _ => new(rows: 4, columns: 6),
        };
    }

    ////////////////////////////////////////////

    public static Item CreateItem()
    {
        return ItemRegistry.Create(AlternativeTextures.TOOL_ID_PAINT_BUCKET);
    }

    public static PaintBucketTool? From(IModHelper helper, Tool? tool)
    {
        return tool is GenericTool { QualifiedItemId: AlternativeTextures.TOOL_ID_PAINT_BUCKET } genericTool
            ? new PaintBucketTool(helper, genericTool)
            : null;
    }
}
