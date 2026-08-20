using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AlternativeTextures.Framework;
using AlternativeTextures.Framework.Models;
using ConsoleLog;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;

namespace AlternativeTextures.Tools;

class PaintBrushEmptyTool(IModHelper helper, GenericTool tool) : ICustomTool
{
    internal const string PAINT_BRUSH_FLAG = "AlternativeTextures.PaintBrushFlag";

    //////////////////////////////////

    public IDisposable? Start()
    {
        return null;
    }

    public IEnumerator<bool> OnButton(ButtonPressedEventArgs e)
    {
        if (e.Button is SButton.MouseRight)
        {
            var tile = new Tile(e.Cursor.Tile);
            PrettyPrint.Log("[MouseRight] Paint Brush");
            DoReadTexture(tile);
            yield return true;
        }
        else if (e.Button.IsUseToolButton())
        {
            var tile = Game1.player.ActiveTargetTile;
            PrettyPrint.Log("[IsUseToolButton] Paint Brush");

            var placedObject = Game1.currentLocation.getObjectAtTile(tile.X, tile.Y);
            if (placedObject?.QualifiedItemId == AlternativeTextures.PAINTPAIL)
            {
                PrettyPrint.Log("Cleaning empty brush using the PAINTPAIL");
                yield return false;
            }
            else
            {
                PrettyPrint.Log("[IsUseToolButton] Getting texture");
                DoReadTexture(tile);
                yield return false;
            }
        }
    }

    ////////////////////////////////////////////

    private void DoReadTexture(Tile tile)
    {
        var paintables = IPaintable.OnTile(tile);
        Console.Log($"paintables: {paintables.Select(x => x.Type).ToList()}");

        var paintableMaybe = IPaintable.OnTile(tile).FirstOrDefault();
        if (paintableMaybe is { } paintable)
        {
            Console.Log($"paintable.TextureIdentifier: {paintable.TextureIdentifier}");
            var item = PaintBrushFilledTool.CreateItem(
                paintable.Type,
                paintable.TextureIdentifier ?? TextureIdentifierWithoutSeason.DefaultFor(paintable.ModelIdentifier)
            );

            var str = JsonSerializer.Serialize(paintable.TextureIdentifier);
            Console.Log($"With: {str}");
            var x = JsonSerializer.Deserialize<TextureIdentifierWithoutSeason>(str);
            Console.Log($"JSON x: {x}");

            Game1.player.Items[Game1.player.CurrentToolIndex] = item;
        }
    }

    ////////////////////////////////////////////

    public static Item CreateItem()
    {
        return ItemRegistry.Create(AlternativeTextures.PAINT_BRUSH_EMPTY_ID);
    }

    public static PaintBrushEmptyTool? From(IModHelper helper, Tool? tool)
    {
        return tool is GenericTool { QualifiedItemId: AlternativeTextures.PAINT_BRUSH_EMPTY_ID } genericTool
            ? new PaintBrushEmptyTool(helper, genericTool)
            : null;
    }
}
