using System;
using System.Collections.Generic;
using System.Linq;
using DralGeometry;
using Incubator;
using Incubator.MonoGame.Drawables;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace AlternativeTextures.App.UI;

internal abstract class LayoutMenu()
    : IClickableMenu(
        0,
        0,
        Game1.graphics.GraphicsDevice.Viewport.Bounds.Width,
        Game1.graphics.GraphicsDevice.Viewport.Bounds.Height
    ),
        ILayout
{
    /// Method to be called when all the hover handlers have been called,
    /// and none of them claim the price, so it should reset back to `null` (or default whatever)
    protected abstract void ClearHover();

    public abstract void Render(ref DrawableBuilder UI);

    /////////////////////////////////////////

    // Computed<DrawableBuilder.Result> RenderSignal;
    Watcher<DrawableBuilder.Result>? RenderWatcher;
    protected DrawableBuilder.Result? RenderResult = null;

    public override void update(GameTime time)
    {
        base.update(time);
        if (RenderWatcher is null)
        {
            RenderWatcher = new(() =>
            {
                var builder = new DrawableBuilder(Game1.graphics.GraphicsDevice.Viewport.Bounds);
                this.Render(ref builder);
                return builder.Finish();
            });
            RenderResult = RenderWatcher.Value;
            /// Very first update
            foreach (var handler in RenderResult?.UpdateHandlers ?? [])
            {
                handler(true);
            }
        }

        foreach (var handler in RenderResult?.TickHandlers ?? [])
        {
            handler(time);
        }

        if (RenderWatcher.HasChanges)
        {
            foreach (var handler in RenderResult?.UpdateHandlers ?? [])
            {
                handler(false);
            }
            RenderResult = RenderWatcher.Run();
        }
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        if (Game1.IsFading())
        {
            ClearHover();
            return;
        }

        var point = new Point(x, y);
        foreach (var handler in RenderResult?.HoverHandlers ?? [])
        {
            if (handler(point))
            {
                return;
            }
        }
        ClearHover();
    }

    public override void receiveKeyPress(Keys key)
    {
        /// TODO Also pass on to menu?
        base.receiveKeyPress(key);
    }

    public override void leftClickHeld(int x, int y)
    {
        /// TODO Also pass on to menu?
        base.leftClickHeld(x, y);
    }

    public override void releaseLeftClick(int x, int y)
    {
        /// TODO Also pass on to menu?
        base.releaseLeftClick(x, y);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        /// TODO Also pass on to menu?
        base.receiveScrollWheelAction(direction);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = false)
    {
        base.receiveLeftClick(x, y, playSound);
        if (Game1.activeClickableMenu == null)
            return;

        var point = new Point(x, y);
        foreach (var handler in RenderResult?.LeftClickHandlers ?? [])
        {
            if (handler(point))
            {
                return;
            }
        }
    }

    public override void applyMovementKey(int directionInt)
    {
        foreach (var handler in RenderResult?.MovementKeyHandlers ?? [])
        {
            handler(Direction.FromNumber(directionInt));
        }
    }

    public override void draw(SpriteBatch batch)
    {
        if (!Game1.dialogueUp && !Game1.IsFading())
        {
            // var UI = new DrawableBuilder(Game1.graphics.GraphicsDevice.Viewport.Bounds);
            new DrawableGroup(RenderResult?.Drawables ?? []).Draw(batch, Game1.graphics.GraphicsDevice.Viewport.Bounds);

            // var builder = new DrawableBuilder(Game1.graphics.GraphicsDevice.Viewport.Bounds, batch);
            // this.Render(builder);
            // builder.Finish();
        }
    }
}

static class ShopCachedThemeSprites
{
    extension(ShopMenu.ShopCachedTheme theme)
    {
        // Summary:
        //     The texture for the shop window border.
        //     This should be an 18x18 pixel area.
        public TextureSprite WindowBorderSprite => theme.WindowBorderTexture.Clip(theme.WindowBorderSourceRect);

        // Summary:
        //     The texture for the NPC portrait background.
        //     This should be a 74x47 pixel area.
        public TextureSprite PortraitBackgroundSprite =>
            theme.PortraitBackgroundTexture.Clip(theme.PortraitBackgroundSourceRect);

        // Summary:
        //     The texture for the NPC dialogue background.
        //     This should be a 60x60 pixel area.
        public TextureSprite DialogueBackgroundSprite =>
            theme.DialogueBackgroundTexture.Clip(theme.DialogueBackgroundSourceRect);

        // Summary:
        //     The texture for the item row background.
        //     This should be a 15x15 pixel area.
        public TextureSprite ItemRowBackgroundSprite =>
            theme.ItemRowBackgroundTexture.Clip(theme.ItemRowBackgroundSourceRect);

        // Summary:
        //     The texture for the item row background when it is being hovered.
        //     This should be a 15x15 pixel area.
        public ClippableSprite ItemRowBackgroundHoverSprite =>
            theme
                .ItemRowBackgroundTexture.Clip(theme.ItemRowBackgroundSourceRect)
                .MultiplyColor(theme.ItemRowBackgroundHoverColor);

        // Summary:
        //     The texture for the box behind the item icons.
        //     This should be a 18x18 pixel area.
        public TextureSprite ItemIconBackgroundSprite =>
            theme.ItemIconBackgroundTexture.Clip(theme.ItemIconBackgroundSourceRect);

        // Summary:
        //     The texture for the scroll up icon.
        //     This should be a 11x12 pixel area.
        public TextureSprite ScrollUpSprite => theme.ScrollUpTexture.Clip(theme.ScrollUpSourceRect);

        // Summary:
        //     The texture for the scroll down icon.
        //     This should be a 11x12 pixel area.
        public TextureSprite ScrollDownSprite => theme.ScrollDownTexture.Clip(theme.ScrollDownSourceRect);

        // Summary:
        //     The texture for the scrollbar foreground texture.
        //     This should be a 6x10 pixel area.
        public TextureSprite ScrollBarFrontSprite => theme.ScrollBarFrontTexture.Clip(theme.ScrollBarFrontSourceRect);

        // Summary:
        //     The texture for the scrollbar background texture.
        //     This should be a 6x6 pixel area.
        public TextureSprite ScrollBarBackSprite => theme.ScrollBarBackTexture.Clip(theme.ScrollBarBackSourceRect);

        /// I'll get to fonts soon enough
        // //
        // // Summary:
        // //     The sprite text color for the dialogue text, or null for the default color.
        // public Color? DialogueColor { get; }

        // //
        // // Summary:
        // //     The sprite text shadow color for the dialogue text, or null for the default color.
        // public Color? DialogueShadowColor { get; }

        // Summary:
        //     The sprite text color for the item text, or null for the default color.
        // public Color? ItemRowTextColor { get; }
    }
}
