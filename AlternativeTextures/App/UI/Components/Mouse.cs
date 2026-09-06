using Incubator.MonoGame;
using Dral.Sprites;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AlternativeTextures.App.UI;

public record Mouse() : IDraw
{
    readonly bool IgnoreTransparency = false;
    readonly int Cursor = -1;

    public void Draw(SpriteBatch batch, Rectangle destination)
    {
        if (!Game1.options.hardwareCursor)
        {
            var num = Game1.mouseCursorTransparency;
            if (IgnoreTransparency)
            {
                num = 1f;
            }

            var cursor = Cursor >= 0 ? Cursor : ((Game1.options.snappyMenus && Game1.options.gamepadControls) ? 44 : 0);

            batch.Draw(
                Game1.mouseCursors,
                new Vector2(Game1.getMouseX(), Game1.getMouseY()),
                Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, cursor, 16, 16),
                Color.White * num,
                0f,
                Vector2.Zero,
                4f + (Game1.dialogueButtonScale / 150f),
                SpriteEffects.None,
                1f
            );
        }
    }
}
