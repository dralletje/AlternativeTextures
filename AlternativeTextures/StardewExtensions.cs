using AlternativeTextures.Framework;
using AlternativeTextures.Tools;
using StardewModdingAPI.Events;
using StardewValley;

namespace AlternativeTextures;

public static class MyExtensions
{
  extension(GameLocation location)
  {
    // public int WordCount() =>
    //     str.Split([' ', '.', '?'], StringSplitOptions.RemoveEmptyEntries).Length;
  }

  extension(Farmer player)
  {
    internal Tile ActiveTargetTile
    {
      get
      {
        if (Game1.wasMouseVisibleThisFrame)
        {
          return new Tile(AlternativeTextures.modHelper.Input.GetCursorPosition().Tile);
        }
        else
        {
          var toolLoc = player.GetToolLocation();
          return new Tile((int)(toolLoc.X / Game1.tileSize), (int)(toolLoc.Y / Game1.tileSize));
        }
      }
    }
  }

  extension(ButtonPressedEventArgs e)
  {
    public bool IsMenuPress
    {
      get
      {
        var screenPixels = e.Cursor.ScreenPixels;
        foreach (var menu in Game1.onScreenMenus)
        {
          if (menu.isWithinBounds((int)screenPixels.X, (int)screenPixels.Y))
          {
            // The player clicked the UI. Bail out and let the vanilla game handle it.
            return true;
          }
        }
        return false;
      }
    }
  }
}