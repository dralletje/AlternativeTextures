using AlternativeTextures.Framework;
using StardewModdingAPI.Events;
using StardewValley;

namespace AlternativeTextures.Stardew;

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
                    return new Tile(ModHelper.shared.Input.GetCursorPosition().Tile);
                }
                else
                {
                    var toolLoc = player.GetToolLocation();
                    return new Tile((int)(toolLoc.X / Game1.tileSize), (int)(toolLoc.Y / Game1.tileSize));
                }
            }
        }
    }

    extension(Season season)
    {
        // public static IEnumerable<Season> All()
        // {
        //     yield return Season.Spring;
        //     yield return Season.Summer;
        //     yield return Season.Fall;
        //     yield return Season.Winter;
        // }

        // public static List<Season> All => [Season.Spring, Season.Summer, Season.Fall, Season.Winter];
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
