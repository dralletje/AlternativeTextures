
using StardewValley;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using System;

namespace AlternativeTextures.CustomToolMod;

file static class Helpers
{
    public static bool IsLevelCursor()
    {
        if (Game1.activeClickableMenu is not null)
            return false;
        if (Game1.currentMinigame is not null || Game1.showingEndOfNightStuff)
            return false;
        if (Game1.options.gamepadControls && !Game1.lastCursorMotionWasMouse)
            return false;
        return true;
    }
}

[HarmonyPatch(typeof(Game1), nameof(Game1.drawMouseCursor))]
public static class DrawMouseCursorPatch
{
    // [HarmonyReversePatch(HarmonyReversePatchType.Original)]
    // [MethodImpl(MethodImplOptions.NoInlining)]
    // public static void Original(SpriteBatch b) => throw new NotImplementedException();

    public static bool Prefix(Game1 __instance)
    {
        if (!Helpers.IsLevelCursor())
            return true;

        if (CustomToolGlobal.Current is null)
            return true;

        // Custom cursor drawing using UI scale:
        var mousePos = Utility.PointToVector2(Game1.getMousePosition());

        if (CustomToolGlobal.Current.DrawMouseCursor(Game1.spriteBatch))
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}
