using System;
using System.Collections.Generic;
using System.Linq;
using DralGeometry;
using Incubator;
using Incubator.MonoGame;
using Incubator.MonoGame.Drawables;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using StardewValley;
using StardewValley.Menus;

namespace AlternativeTextures.App.UI;

internal class GridMenu : IClickableMenu
{
    internal interface Item : IDraw
    {
        public string? DisplayName { get; init; }
        public string? HoverText { get; init; }
        // public void Draw(SpriteBatch batch, Rectangle destinationRect);
    }

    readonly ICollection<Item> items;
    readonly GridSize gridSize;
    readonly string _title;
    readonly Action<Item>? onPress;

    // private Dictionary<string, Texture2D> _skinIdToTextures = [];
    // private Dictionary<string, Texture2D> _breedIdToTextures = [];

    int VirtualRows
    {
        get { return (int)Math.Ceiling((double)items.Count / gridSize.Columns); }
    }

    State<Item?> hovered = new(null);
    State<int> rowsScrolled = new(0);

    // Computed<DrawableBuilder.Result> RenderSignal;
    Watcher<DrawableBuilder.Result> RenderWatcher;
    DrawableBuilder.Result? LastResult = null;

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
            LocalizedContentManager.CurrentLanguageCode
            is LocalizedContentManager.LanguageCode.ko
                or LocalizedContentManager.LanguageCode.fr
        )
        {
            base.height += 64;
        }

        // var topLeft = Utility.getTopLeftPositionForCenteringOnScreen(base.width, base.height);
        // base.xPositionOnScreen = (int)topLeft.X;
        // base.yPositionOnScreen = (int)topLeft.Y;

        RenderWatcher = new(() =>
        {
            var builder = new DrawableBuilder(Game1.graphics.GraphicsDevice.Viewport.Bounds);
            this.Render(ref builder);
            return builder.Finish();
        });
        // LastResult = RenderWatcher.Value;
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
        if (gridSize.Coords().ElementAtOrDefault(indexRelativeToRow) is { } coord)
        {
            if (Game1.options.SnappyMenus)
            {
                // var coord = gridSize.PositionForIndex(indexRelativeToRow);
                if (gridSize.Coords().Contains(coord))
                {
                    Focus.Value = new FocusElement.InGrid(coord);
                }
                else
                {
                    Focus.Value = new FocusElement.InGrid((0, 0));
                }
            }
        }
    }

    public override void update(GameTime time)
    {
        if (LastResult is null)
        {
            LastResult = RenderWatcher.Value;
            /// Very first update
            foreach (var handler in LastResult?.UpdateHandlers ?? [])
            {
                handler(true);
            }
        }

        foreach (var handler in LastResult?.TickHandlers ?? [])
        {
            handler(time);
        }

        if (RenderWatcher.HasChanges)
        {
            foreach (var handler in LastResult?.UpdateHandlers ?? [])
            {
                handler(false);
            }
            LastResult = RenderWatcher.Run();
        }
    }

    protected void snapBehavior(Direction direction, (int column, int row) coords)
    {
        var (column, row) = coords;
        var index = (row * gridSize.Columns) + row;

        var isFirstRow = row == 0;
        var isLastRow = row + 1 == gridSize.Rows;

        var isFirstColumn = column == 0;
        var isLastColumn = column + 1 == gridSize.Columns;

        var canScrollMore = rowsScrolled + gridSize.Rows < VirtualRows;
        var itemsScrolled = rowsScrolled * gridSize.Columns;

        var gridCoords = gridSize.Coords();

        switch (direction)
        {
            case Direction.Down:
            {
                if (isLastRow && canScrollMore)
                {
                    rowsScrolled.Value++;
                }
                else if (itemsScrolled + index + gridSize.Columns < items.Count)
                {
                    if (Game1.options.SnappyMenus)
                    {
                        Focus.Value = new FocusElement.InGrid((column, row + 1));
                    }
                }
                else
                {
                    /// Do something at the bottom of the bottom?
                }
                break;
            }
            case Direction.Up:
            {
                if (isFirstRow && rowsScrolled > 0)
                {
                    rowsScrolled.Value--;
                }
                else if (itemsScrolled + index - gridSize.Columns >= 0)
                {
                    if (Game1.options.SnappyMenus)
                    {
                        Focus.Value = new FocusElement.InGrid((column, row - 1));
                    }
                }
                else
                {
                    /// Do something at the top of the top?
                }
                break;
            }
            case Direction.Left:
            {
                var to = (column - 1, row);
                if (gridCoords.Contains(to))
                {
                    Focus.Value = new FocusElement.InGrid(to);
                }
                break;
            }
            case Direction.Right:
            {
                var to = (column + 1, row);
                if (gridCoords.Contains(to))
                {
                    Focus.Value = new FocusElement.InGrid(to);
                }
                else if (row + (1 / gridSize.Rows) > 0.5)
                {
                    Focus.Value = new FocusElement.InScrollbar(VerticalScrollbar.FocusElement.DownArrow);
                }
                else
                {
                    Focus.Value = new FocusElement.InScrollbar(VerticalScrollbar.FocusElement.UpArrow);
                }
                break;
            }
        }
    }

    public override void performHoverAction(int x, int y)
    {
        if (Game1.IsFading())
        {
            hovered.Value = null;
            return;
        }

        Item? hovering = null;
        var point = new Point(x, y);
        foreach (var handler in LastResult?.HoverHandlers ?? [])
        {
            if (handler(point))
            {
                return;
            }
        }
        hovered.Value = hovering;
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
            return;

        var point = new Point(x, y);
        foreach (var handler in LastResult?.LeftClickHandlers ?? [])
        {
            if (handler(point))
            {
                return;
            }
        }

        // foreach (var (button, item) in elementsOnScreen)
        // {
        //     if (!button.containsPoint(x, y))
        //         continue;

        //     if (this.onPress is { } onPress)
        //     {
        //         onPress(item);
        //         this.exitThisMenu();
        //     }

        //     return;
        // }

        // if (downArrow.containsPoint(x, y) && rowsScrolled < Math.Max(0, VirtualRows - gridSize.Rows))
        // {
        //     downArrowPressed();
        //     Game1.playSound("shwip");
        // }
        // else if (upArrow.containsPoint(x, y) && rowsScrolled > 0)
        // {
        //     upArrowPressed();
        //     Game1.playSound("shwip");
        // }
        // else if (scrollBar.containsPoint(x, y))
        // {
        //     scrolling = true;
        // }
        // else if (
        //     !downArrow.containsPoint(x, y)
        //     && x > xPositionOnScreen + width
        //     && x < xPositionOnScreen + width + 128
        //     && y > yPositionOnScreen
        //     && y < yPositionOnScreen + height
        // )
        // {
        //     scrolling = true;
        //     leftClickHeld(x, y);
        //     releaseLeftClick(x, y);
        // }
    }

    private readonly State<bool> IsScrolling = new(false);

    private void downArrowPressed()
    {
        rowsScrolled.Value++;
    }

    private void upArrowPressed()
    {
        rowsScrolled.Value--;
    }

    public override void leftClickHeld(int x, int y)
    {
        base.leftClickHeld(x, y);
        if (IsScrolling)
        {
            // var num = (float)(y - scrollBarRunner.Y) / (float)scrollBarRunner.Height;
            // var newRowsScrolled = Math.Min(
            //     Math.Max(0, VirtualRows - gridSize.Rows),
            //     Math.Max(0, (int)((float)VirtualRows * num))
            // );
            // if (rowsScrolled != scrollBar.bounds.Y)
            // {
            //     Game1.playSound("shiny4");
            //     rowsScrolled.Value = newRowsScrolled;
            // }
        }
    }

    public override void applyMovementKey(int directionInt)
    {
        foreach (var handler in LastResult?.MovementKeyHandlers ?? [])
        {
            handler(Direction.FromNumber(directionInt));
        }
    }

    public override void releaseLeftClick(int x, int y)
    {
        base.releaseLeftClick(x, y);
        IsScrolling.Value = false;
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

    record InScrollbarLens(IState<FocusElement?> state) : IState<VerticalScrollbar.FocusElement?>
    {
        public VerticalScrollbar.FocusElement? Value
        {
            get => state.Value is FocusElement.InScrollbar(var x) ? x : null;
            set
            {
                if (value is { } notnull)
                {
                    state.Value = new FocusElement.InScrollbar(notnull);
                }
                else
                {
                    state.Value = null;
                }
            }
        }
    }

    // [Union]
    public partial record FocusElement
    {
        // 3. Define the union variants as inner partial records.
        public partial record InScrollbar(VerticalScrollbar.FocusElement focusElement)
            : FocusElement,
                IVariant<InScrollbar, FocusElement, VerticalScrollbar.FocusElement>
        {
            public VerticalScrollbar.FocusElement Extract() => this.focusElement;

            public static InScrollbar Pack(VerticalScrollbar.FocusElement inner) => new InScrollbar(inner);

            public static IState<VerticalScrollbar.FocusElement> Lens(IState<FocusElement?> container) =>
                new VariantLens<FocusElement, VerticalScrollbar.FocusElement, InScrollbar>(container);
        }

        public partial record InGrid((int column, int row) Element) : FocusElement;
    }

    // State<FocusElement?> Focus = new(null);
    State<FocusElement?> Focus = new(new FocusElement.InScrollbar(VerticalScrollbar.FocusElement.UpArrow));

    TextureSprite ItemRowBackground =>
        VisualTheme.ItemRowBackgroundTexture.Clip(VisualTheme.ItemRowBackgroundSourceRect);
    TextureSprite MouseCursorOrSomethingSprite = Game1.mouseCursors.Clip(new Rectangle(384, 373, 18, 18));

    public void Render(ref DrawableBuilder UI)
    {
        var topLeft = Utility.getTopLeftPositionForCenteringOnScreen(base.width, base.height);
        var Bounds = new Rectangle((int)topLeft.X, (int)topLeft.Y, base.width, base.height);

        UI += Game1.fadeToBlackRect.MultiplyColor(Color.Black * 0.75f);
        using (UI.Group(Bounds))
        {
            UI += new StringWithScrollCenteredAt(_title).At((1f / 2).Pc, -64);

            UI += new TextureBox(MouseCursorOrSomethingSprite);

            var menuPadding = new Padding(all: 16) { Top = 20, Right = 12 } + new Padding(all: 16);
            using (UI.Group(UI.Frame - menuPadding))
            {
                var currentPageItems = items.Skip(rowsScrolled * gridSize.Columns).Take(gridSize.Count);
                foreach (var (coord, item) in GridLayout.Create(UI, gridSize, currentPageItems))
                {
                    UI += new TextureBox()
                    {
                        Texture = ItemRowBackground,
                        Color =
                            (this.hovered.Value == item && !IsScrolling)
                                ? VisualTheme.ItemRowBackgroundHoverColor
                                : Color.White,
                        DrawShadow = false,
                    };

                    UI += item.Padding(new Padding(all: 12));

                    if (this.onPress is { } onPress)
                    {
                        UI += new OnEventsLayout()
                        {
                            OnLeftClick = () =>
                            {
                                onPress(item);
                                this.exitThisMenu();
                                return true;
                            },
                        };
                    }

                    UI += new FocusHandlerLayoutClass<FocusElement>(Focus, new FocusElement.InGrid(coord))
                    {
                        OnUnhandledMove = (direction) => snapBehavior(direction, coord),
                    };

                    var currentFrame = UI.Frame;
                    UI.OnHover += (point) =>
                    {
                        if (currentFrame.Contains(point))
                        {
                            hovered.Value = item;
                            return true;
                        }
                        return false;
                    };
                }
            }

            // using (var Column = new VStack(UI))
            // {
            //     using (Column.Item(1)) { }

            //     using (Column.Item(1)) { }

            //     using (Column.Item(1)) { }
            // }

            if (items.Count > gridSize.Count)
            {
                var scrollProgress =
                    VirtualRows == 0 ? 0 : Math.Clamp((float)rowsScrolled / (VirtualRows - gridSize.Rows), 0, 1);
                var frame =
                    new Rectangle(
                        x: UI.Frame.X + UI.Frame.Width + 16,
                        y: UI.Frame.Y,
                        width: 48,
                        height: UI.Frame.Height
                    ) - new Padding(vertical: 16);

                UI += new VerticalScrollbar(Progress: scrollProgress)
                {
                    // Focus = FocusElement.InScrollbar.Lens(Focus),
                    Focus = new InScrollbarLens(Focus),
                    OnUpClick = this.upArrowPressed,
                    OnDownClick = this.downArrowPressed,
                    OnThumbClick = () =>
                    {
                        this.IsScrolling.Value = true;
                    },
                    OnUnhandledMove = (from, direction) =>
                    {
                        Console.Log($"Unhandled move from scrollbar {direction}");
                        var gridCoords = gridSize.Coords();
                        if (direction is Direction.Left)
                        {
                            var startindex = from switch
                            {
                                VerticalScrollbar.FocusElement.UpArrow => gridSize.Columns - 1,
                                VerticalScrollbar.FocusElement.Thumb => (gridSize.Rows / 2) * gridSize.Columns - 1,
                                VerticalScrollbar.FocusElement.DownArrow => gridSize.Count - 1,
                            };

                            var itemsInCurrentView = items.Count - (rowsScrolled * gridSize.Columns);
                            startindex = Math.Clamp(startindex, 0, itemsInCurrentView - 1);
                            var coord = gridSize.PositionForIndex(startindex);
                            Focus.Value = new FocusElement.InGrid(coord);
                        }
                    },
                }.Frame(frame);
            }

            if (hovered?.Value?.DisplayName is { } displayName)
            {
                UI += new StringWithScrollCenteredAt(displayName, "").At(x: (1f / 2).Pc, y: 1f.Pc);
            }
        }

        if (this.hovered?.Value?.HoverText is { } hoverText)
        {
            UI += new HoverText(hoverText);
        }

        UI += new Mouse();
    }

    public override void draw(SpriteBatch batch)
    {
        if (!Game1.dialogueUp && !Game1.IsFading())
        {
            // var UI = new DrawableBuilder(Game1.graphics.GraphicsDevice.Viewport.Bounds);
            new DrawableGroup(LastResult?.Drawables ?? []).Draw(batch, Game1.graphics.GraphicsDevice.Viewport.Bounds);

            // var builder = new DrawableBuilder(Game1.graphics.GraphicsDevice.Viewport.Bounds, batch);
            // this.Render(builder);
            // builder.Finish();
        }
    }
}
