using System.Diagnostics.CodeAnalysis;
using Dral.Sprites;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace AlternativeTextures.App.UI;

public record HoverText(string Text, SpriteFont? Font = null) : IDraw
{
    public void Draw(SpriteBatch batch, Rectangle destination)
    {
        IClickableMenu.drawHoverText(batch, Text, Font ?? Game1.smallFont);
    }
}

public record TextureBox() : IDraw
{
    [SetsRequiredMembers]
    public TextureBox(TextureSprite texture)
        : this()
    {
        Texture = texture;
    }

    public required TextureSprite Texture;

    public Color Color = Color.White;
    public bool DrawShadow = true;

    public void Draw(SpriteBatch batch, Rectangle destination)
    {
        IClickableMenu.drawTextureBox(
            batch,
            Texture.Texture,
            Texture.SourceRect,
            destination.X,
            destination.Y,
            destination.Width,
            destination.Height,
            Color,
            4f,
            DrawShadow
        );
    }
}

public record StringWithScrollCenteredAt(string Title, string? placeHolderWidthText = null) : IDraw
{
    public void Draw(SpriteBatch batch, Rectangle destination)
    {
        SpriteText.drawStringWithScrollCenteredAt(
            batch,
            Title,
            destination.X,
            destination.Y,
            placeHolderWidthText ?? ""
        );
    }
}

public static class StardewSprites
{
    public static TextureSprite CloseSprite = Game1.mouseCursors.Clip(new Rectangle(337, 494, 12, 12));
    public static TextureSprite PlacementSquare = Game1.mouseCursors.Clip(new Rectangle(194, 388, 16, 16));
}
