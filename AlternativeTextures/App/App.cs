using System;
using System.Collections.Generic;
using AlternativeTextures.App.Tools;
using AlternativeTextures.App.UI;
using AlternativeTextures.CustomToolMod;
using AlternativeTextures.MetaFramework;
using Dral.Sprites;
using HarmonyLib;
using Incubator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace AlternativeTextures.App;

class App<TParent>(DralModContext<TParent, App<TParent>> context) : DralMod
    where TParent : HasMod<ICustomToolMod>
{
    IMonitor Monitor = context.Monitor;
    IModHelper Helper = context.Helper;
    IManifest ModManifest = context.ModManifest;

    public override IDisposable? Entry()
    {
        try
        {
            var harmony = new Harmony(ModManifest.UniqueID);
            // Paint tool related patches
            new ToolPatch(Helper).Apply(harmony);
        }
        catch (Exception e)
        {
            Monitor.Log($"Issue with Harmony patching: {e}", LogLevel.Error);
        }

        var customToolMod = context.Parent.GetMod<ICustomToolMod>();

        customToolMod.Register(
            AlternativeTextures.PAINT_BRUSH_EMPTY_ID,
            (tool) => new PaintBrushEmptyTool(Helper, tool)
        );
        customToolMod.Register(
            AlternativeTextures.PAINT_BRUSH_FILLED_ID,
            (tool) => new PaintBrushFilledTool(Helper, tool)
        );
        customToolMod.Register(AlternativeTextures.TOOL_ID_PAINT_BUCKET, (tool) => new PaintBucketTool(Helper, tool));

        Helper.ConsoleCommands.Add(
            name: "my_command",
            documentation: "Prints a message to the console.\n\nUsage: my_command <message>",
            callback: OnCommandInvoked
        );
        return new ActionDisposable(() => { });
    }

    record AnyGridItem(string? Title, ISprite Sprite) : GridMenu.Item, IDraw
    {
        public AnyGridItem(ISprite Sprite) : this(null, Sprite) { }

        public string? DisplayName => Title;
        public string? HoverText { get; init; }
        void IDraw.Draw(SpriteBatch spriteBatch, Rectangle destinationRect)
        {
            var littleInset =
                destinationRect
                - new Padding()
                {
                    Top = 4,
                    Bottom = 0,
                    Left = 8,
                    Right = 8,
                };

            var Width = Sprite.Width * Game1.pixelZoom;
            var Height = Sprite.Height * Game1.pixelZoom;

            var x = new Rectangle(
                (littleInset.X + (littleInset.Width / 2)) - (Width / 2),
                (littleInset.Y + (littleInset.Height / 2)) - (Height / 2),
                Width,
                Height
            );


            Sprite.Draw(spriteBatch, x);
        }
    }

    private void OnCommandInvoked(string command, string[] args)
    {
        List<GridMenu.Item> xs = [
            new AnyGridItem(Game1.mouseCursors_1_6.Clip(new(498, 368, 13, 9))),
            new AnyGridItem("Clock small hand", Game1.mouseCursors.Clip(Town.hourHandSource)),
            new AnyGridItem("Clock big hand", Game1.mouseCursors.Clip(Town.minuteHandSource)),
            new AnyGridItem("Clock Nub", Game1.mouseCursors.Clip(Town.clockNub)),
            new AnyGridItem("Text Balloon", Game1.mouseCursors.Clip(new(141, 465, 20, 24))),
            new AnyGridItem("Under construction Sign", Game1.mouseCursors.Clip(new(367, 309, 16, 15))),
            new AnyGridItem("", Game1.mouseCursors2.Clip(new(96, 48, 16, 16))),
            new AnyGridItem("", Game1.mouseCursors2.Clip(new(80, 48, 16, 16))),
            new AnyGridItem("", Game1.mouseCursors2.Clip(new(64, 48, 16, 16))),
            new AnyGridItem("", Game1.mouseCursors2.Clip(new(64, 64, 16, 16))),
            new AnyGridItem("", Game1.mouseCursors2.Clip(new(96, 64, 16, 16))),
            new AnyGridItem("", Game1.mouseCursors2.Clip(new(80, 64, 16, 16))),
        ];
        var gridMenu = new GridMenu(
            xs,
            new(rows: 3, columns: 5),
            uiTitle: "Textures"
        );

        Game1.activeClickableMenu = gridMenu;
    }
}
