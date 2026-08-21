using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AlternativeTextures.CustomToolMod;
using AlternativeTextures.Framework;
using Incubator;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;

namespace AlternativeTextures.App.Tools;

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
            Console.Log($"[MouseRight] Paint Brush");
            DoReadTexture(tile);
            yield return true;
        }
        else if (e.Button.IsUseToolButton())
        {
            var tile = Game1.player.ActiveTargetTile;
            Console.Log($"[IsUseToolButton] Paint Brush {tile}");

            var placedObject = Game1.currentLocation.getObjectAtTile(tile.X, tile.Y);
            if (placedObject?.QualifiedItemId == AlternativeTextures.PAINTPAIL)
            {
                Console.Log($"Cleaning empty brush using the PAINTPAIL");
                yield return false;
            }
            else
            {
                Console.Log($"[IsUseToolButton] Getting texture");
                DoReadTexture(tile);
                yield return false;
            }
        }
    }

    ////////////////////////////////////////////

    private void DoReadTexture(Tile tile)
    {
        var paintableMaybe = IPaintable
            .GetAnythingAtTile(tile)
            .Select(x => IPaintable.From(x))
            .WhereNotNull()
            .FirstOrDefault();
        if (paintableMaybe is { } paintable)
        {
            var item = PaintBrushFilledTool.CreateItem(
                paintable.TextureIdentifier ?? TextureIdentifierWithoutSeason.DefaultFor(paintable.ModelIdentifier)
            );

            Game1.player.Items[Game1.player.CurrentToolIndex] = item;
        }
    }

    ////////////////////////////////////////////

    public static Item CreateItem()
    {
        return ItemRegistry.Create(AlternativeTextures.PAINT_BRUSH_EMPTY_ID);
    }
}
