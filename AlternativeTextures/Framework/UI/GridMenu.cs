using System;
using System.Collections.Generic;
using System.Linq;
using AlternativeTextures.Incubator;
using ConsoleLog;
using DralGeometry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using static AlternativeTextures.Framework.Models.AlternativeTextureModel;

namespace AlternativeTextures.Framework.UI;

internal class GridMenu : IClickableMenu
{
    internal interface Item
    {
        public string? DisplayName { get; init; }
        public void Draw(SpriteBatch batch, Rectangle destinationRect);
    }

    readonly ICollection<Item> items;
    readonly GridSize gridSize;
    readonly string _title;
    readonly Action<Item>? onPress;

    public Item? hovered;
    protected Signal<int> rowsScrolled = new(0);
    Computed<int> scrollbarY;
    public List<ClickableComponent> itemGrid = [];

    // private Dictionary<string, Texture2D> _skinIdToTextures = [];
    // private Dictionary<string, Texture2D> _breedIdToTextures = [];

    int VirtualRows
    {
        get { return (int)Math.Ceiling((double)items.Count / gridSize.Columns); }
    }

    ClickableTextureComponent upArrow;
    ClickableTextureComponent downArrow;
    ClickableTextureComponent scrollBar;
    Rectangle scrollBarRunner;

    ShopMenu.ShopCachedTheme VisualTheme = new(null);

    public GridMenu(
        ICollection<Item> items,
        GridSize gridSize,
        string uiTitle = "Paint Bucket",
        Action<Item>? onPress = null
    )
        : base(0, 0, 832, 576, showUpperRightCloseButton: true)
    {
        this.items = items;
        this.gridSize = gridSize;
        this._title = uiTitle;
        this.onPress = onPress;

        // Set up menu structure
        if (
            LocalizedContentManager.CurrentLanguageCode is LocalizedContentManager.LanguageCode.ko
            or LocalizedContentManager.LanguageCode.fr
        )
        {
            base.height += 64;
        }

        var topLeft = Utility.getTopLeftPositionForCenteringOnScreen(base.width, base.height);
        base.xPositionOnScreen = (int)topLeft.X;
        base.yPositionOnScreen = (int)topLeft.Y;

        /////////////////////////////////////

        var borderInset = new Padding(all: 16) { Top = 20, Right = 12 };
        var padding = new Padding(all: 16);
        var menuarea = new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height);
        var buttonarea = menuarea - borderInset - padding;
        var buttonWidth = buttonarea.Width / gridSize.Columns;
        var buttonHeight = buttonarea.Height / gridSize.Rows;

        foreach (var row in Enumerable.Range(0, gridSize.Rows))
        {
            foreach (var column in Enumerable.Range(0, gridSize.Columns))
            {
                var componentId = column + (row * gridSize.Columns);
                this.itemGrid.Add(
                    new ClickableComponent(
                        new Rectangle(
                            buttonarea.X + (buttonWidth * column),
                            buttonarea.Y + (buttonHeight * row),
                            buttonWidth,
                            buttonHeight
                        ),
                        ""
                    )
                    {
                        myID = componentId,
                        downNeighborID = ClickableComponent.CUSTOM_SNAP_BEHAVIOR,
                        upNeighborID = ClickableComponent.CUSTOM_SNAP_BEHAVIOR,
                        rightNeighborID =
                            column == (gridSize.Columns - 1) ? ClickableComponent.SNAP_AUTOMATIC : componentId + 1,
                        leftNeighborID = column > 0 ? componentId - 1 : ClickableComponent.SNAP_AUTOMATIC,
                    }
                );
            }
        }

        /////////////////////////////////////

        upArrow = new ClickableTextureComponent(
            new Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen + 16, 44, 48),
            VisualTheme.ScrollUpTexture,
            VisualTheme.ScrollUpSourceRect,
            4f
        )
        {
            myID = 97865,
            downNeighborID = 106,
            leftNeighborID = 3546,
        };
        downArrow = new ClickableTextureComponent(
            new Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen + height - 64, 44, 48),
            VisualTheme.ScrollDownTexture,
            VisualTheme.ScrollDownSourceRect,
            4f
        )
        {
            myID = 106,
            upNeighborID = 97865,
            leftNeighborID = 3546,
        };

        scrollBar = new ClickableTextureComponent(
            new Rectangle(upArrow.bounds.X + 12, upArrow.bounds.Y + upArrow.bounds.Height + 4, 24, 40),
            VisualTheme.ScrollBarFrontTexture,
            VisualTheme.ScrollBarFrontSourceRect,
            4f
        );
        scrollBarRunner = new Rectangle(
            scrollBar.bounds.X,
            upArrow.bounds.Bottom + 4,
            scrollBar.bounds.Width,
            this.height - 64 - upArrow.bounds.Height - 28
        );

        ////////////////////////////////////////////

        // Call snap functions
        if (Game1.options.SnappyMenus)
        {
            base.populateClickableComponentList();
            this.setCurrentlySnappedComponentTo(0);
            this.snapCursorToCurrentSnappedComponent();
        }

        this.scrollbarY = new(() =>
        {
            var listProgress =
                VirtualRows == 0 ? 0 : Math.Clamp((float)rowsScrolled / (VirtualRows - gridSize.Rows), 0, 1);
            var moveableHeight = scrollBarRunner.Height - scrollBar.bounds.Height;
            return (int)(scrollBarRunner.Top + (moveableHeight * listProgress));
        });
    }

    public void ScrollTo(int index)
    {
        if (index >= items.Count)
        {
            return;
        }

        var row = Math.Min(
            /// Row we want to see
            index / gridSize.Columns,
            /// Last scrollable row
            VirtualRows - gridSize.Rows
        );
        rowsScrolled.Value = row;

        var indexRelativeToRow = index - (row * gridSize.Columns);
        if (itemGrid.ElementAtOrDefault(indexRelativeToRow) is { } element)
        {
            this.currentlySnappedComponent = element;
            this.snapCursorToCurrentSnappedComponent();
        }
    }

    static class Direction
    {
        public const int UP = 0;
        public const int DOWN = 2;
    }

    protected override void customSnapBehavior(int direction, int oldRegion, int oldID)
    {
        var thisRow = (oldID) / gridSize.Columns;
        var isFirstRow = thisRow == 0;
        var isLastRow = thisRow + 1 == gridSize.Rows;
        if (direction is Direction.DOWN)
        {
            if (isLastRow && rowsScrolled + gridSize.Rows < VirtualRows)
            {
                rowsScrolled.Value++;
            }
            else if ((rowsScrolled * gridSize.Columns) + oldID + gridSize.Columns < items.Count)
            {
                this.currentlySnappedComponent = this.getComponentWithID(oldID + gridSize.Columns);
                this.snapCursorToCurrentSnappedComponent();
            }
            else
            {
                /// Do something at the bottom of the bottom?
            }
        }
        else if (direction is Direction.UP)
        {
            if (isFirstRow && rowsScrolled > 0)
            {
                rowsScrolled.Value--;
            }
            else if ((rowsScrolled * gridSize.Columns) + oldID - gridSize.Columns >= 0)
            {
                this.currentlySnappedComponent = this.getComponentWithID(oldID - gridSize.Columns);
                this.snapCursorToCurrentSnappedComponent();
            }
            else
            {
                /// Do something at the top of the top?
            }
        }

        base.customSnapBehavior(direction, oldRegion, oldID);
    }

    public override void performHoverAction(int x, int y)
    {
        this.hovered = null;
        if (Game1.IsFading())
        {
            return;
        }

        foreach (var (button, menuItem) in elementsOnScreen)
        {
            if (button.containsPoint(x, y))
            {
                this.hovered = menuItem;
            }
        }
    }

    public override void receiveKeyPress(Keys key)
    {
        PrettyPrint.Log("KeyPress", key);
        base.receiveKeyPress(key);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = false)
    {
        base.receiveLeftClick(x, y, playSound);
        if (Game1.activeClickableMenu == null)
        {
            return;
        }

        foreach (var (button, item) in elementsOnScreen)
        {
            if (!button.containsPoint(x, y))
                continue;

            if (this.onPress is { } onPress)
            {
                onPress(item);
                this.exitThisMenu();
            }

            return;
        }

        if (downArrow.containsPoint(x, y) && rowsScrolled < Math.Max(0, VirtualRows - gridSize.Rows))
        {
            downArrowPressed();
            Game1.playSound("shwip");
        }
        else if (upArrow.containsPoint(x, y) && rowsScrolled > 0)
        {
            upArrowPressed();
            Game1.playSound("shwip");
        }
        else if (scrollBar.containsPoint(x, y))
        {
            scrolling = true;
        }
        else if (
            !downArrow.containsPoint(x, y)
            && x > xPositionOnScreen + width
            && x < xPositionOnScreen + width + 128
            && y > yPositionOnScreen
            && y < yPositionOnScreen + height
        )
        {
            scrolling = true;
            leftClickHeld(x, y);
            releaseLeftClick(x, y);
        }
    }

    private bool scrolling = false;

    private void downArrowPressed()
    {
        downArrow.scale = downArrow.baseScale;
        rowsScrolled.Value++;
    }

    private void upArrowPressed()
    {
        upArrow.scale = upArrow.baseScale;
        rowsScrolled.Value--;
    }

    public override void leftClickHeld(int x, int y)
    {
        base.leftClickHeld(x, y);
        if (scrolling)
        {
            var y2 = scrollBar.bounds.Y;
            scrollBar.bounds.Y = Math.Min(
                yPositionOnScreen + height - 64 - 12 - scrollBar.bounds.Height,
                Math.Max(y, yPositionOnScreen + upArrow.bounds.Height + 20)
            );
            var num = (float)(y - scrollBarRunner.Y) / (float)scrollBarRunner.Height;
            rowsScrolled.Value = Math.Min(
                Math.Max(0, VirtualRows - gridSize.Rows),
                Math.Max(0, (int)((float)VirtualRows * num))
            );
            if (y2 != scrollBar.bounds.Y)
            {
                Game1.playSound("shiny4");
            }
        }
    }

    public override void releaseLeftClick(int x, int y)
    {
        base.releaseLeftClick(x, y);
        scrolling = false;
    }

    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);
        if (direction > 0 && rowsScrolled > 0)
        {
            rowsScrolled.Value--;
            Game1.playSound("shiny4");
        }
        else if (direction < 0 && (gridSize.Rows + rowsScrolled) < VirtualRows)
        {
            rowsScrolled.Value++;
            Game1.playSound("shiny4");
        }
    }

    IEnumerable<(ClickableComponent, Item)> elementsOnScreen
    {
        get
        {
            var pageOffset = rowsScrolled * gridSize.Columns;
            return Enumerable.Zip(this.itemGrid, this.items.Skip(pageOffset));
        }
    }

    public override void draw(SpriteBatch batch)
    {
        if (!Game1.dialogueUp && !Game1.IsFading())
        {
            batch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
            SpriteText.drawStringWithScrollCenteredAt(
                batch,
                _title,
                base.xPositionOnScreen + base.width / 4,
                base.yPositionOnScreen - 64
            );
            IClickableMenu.drawTextureBox(
                batch,
                Game1.mouseCursors,
                new Rectangle(384, 373, 18, 18),
                base.xPositionOnScreen,
                base.yPositionOnScreen,
                base.width,
                base.height,
                Color.White,
                4f
            );

            foreach (var (button, option) in elementsOnScreen)
            {
                IClickableMenu.drawTextureBox(
                    batch,
                    VisualTheme.ItemRowBackgroundTexture,
                    VisualTheme.ItemRowBackgroundSourceRect,
                    button.bounds.X,
                    button.bounds.Y,
                    button.bounds.Width,
                    button.bounds.Height,
                    (button.containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY()) && !scrolling)
                        ? VisualTheme.ItemRowBackgroundHoverColor
                        : Color.White,
                    4f,
                    drawShadow: false
                );

                var borderInset = new Padding(all: 12);
                var littleInset = button.bounds - borderInset;
                option.Draw(batch, littleInset);
            }

            if (items.Count > gridSize.Count)
            {
                upArrow.draw(batch);
                downArrow.draw(batch);
                IClickableMenu.drawTextureBox(
                    batch,
                    VisualTheme.ScrollBarBackTexture,
                    VisualTheme.ScrollBarBackSourceRect,
                    scrollBarRunner.X,
                    scrollBarRunner.Y,
                    scrollBarRunner.Width,
                    scrollBarRunner.Height,
                    Color.White,
                    4f
                );

                scrollBar.bounds.Y = scrollbarY;
                scrollBar.draw(batch);
            }
        }

        /// TODO Hover
        if (this.hovered?.DisplayName is { } text)
        {
            // var technicalName = $"{hovered.TextureIdentifier.Owner} > {hovered.TextureIdentifier.Variation + 1}";
            // if (hovered.DisplayName is { } displayName)
            // {
            //     hoverInfoText = technicalName;
            //     hoverDisplayName = displayName;
            // }
            // else
            // {
            //     hoverDisplayName = technicalName;
            // }
            SpriteText.drawStringWithScrollCenteredAt(
                batch,
                text,
                Game1.uiViewport.Width / 2,
                base.yPositionOnScreen + base.height + 16,
                "Hover over an item to see its texture name!"
            );
        }

        // if (!String.IsNullOrEmpty(hoverInfoText))
        // {
        //     IClickableMenu.drawHoverText(batch, hoverInfoText, Game1.smallFont);
        // }

        Game1.mouseCursorTransparency = 1f;
        base.drawMouse(batch);
    }
}
