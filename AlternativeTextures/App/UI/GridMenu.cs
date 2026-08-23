using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ConsoleLog;
using DralGeometry;
using Incubator;
using Incubator.MonoGame;
using Incubator.MonoGame.Drawables;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using StardewValley;
using StardewValley.BellsAndWhistles;
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

    Signal<Item?> hovered = new(null);
    Signal<int> rowsScrolled = new(0);
    Computed<int> scrollbarY;
    Computed<List<IDraw>> RenderSignal;

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
                        // new Rectangle((buttonWidth * column), (buttonHeight * row), buttonWidth, buttonHeight),
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
            return (int)(moveableHeight * listProgress);
        });
        this.RenderSignal = new(() =>
        {
            var builder = new DrawableBuilder();
            this.Render(builder);
            return builder.Finish();
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
            if (Game1.options.SnappyMenus)
            {
                this.currentlySnappedComponent = element;
                this.snapCursorToCurrentSnappedComponent();
            }
        }
    }

    static class Direction
    {
        public const int UP = 0;
        public const int DOWN = 2;
    }

    protected override void customSnapBehavior(int direction, int oldRegion, int oldID)
    {
        var thisRow = oldID / gridSize.Columns;
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
                if (Game1.options.SnappyMenus)
                {
                    this.setCurrentlySnappedComponentTo(oldID + gridSize.Columns);
                    this.snapCursorToCurrentSnappedComponent();
                }
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
                if (Game1.options.SnappyMenus)
                {
                    this.setCurrentlySnappedComponentTo(oldID - gridSize.Columns);
                    this.snapCursorToCurrentSnappedComponent();
                }
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
        if (Game1.IsFading())
        {
            hovered.Value = null;
            return;
        }

        Item? hovering = null;
        foreach (var (button, menuItem) in elementsOnScreen)
        {
            if (button.containsPoint(x, y))
            {
                if (hovered != menuItem)
                {
                    hovering = menuItem;
                }
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

    IEnumerable<(ClickableComponent button, Item item)> elementsOnScreen
    {
        get
        {
            var pageOffset = rowsScrolled * gridSize.Columns;
            return Enumerable.Zip(this.itemGrid, this.items.Skip(pageOffset));
        }
    }

    record Grid(GridSize Size, IEnumerable<IDraw> Children) : IDraw
    {
        public Grid(GridSize size, Action<DrawableBuilder> ChildrenFn)
            : this(size, DrawableBuilder.Create(ChildrenFn)) { }

        public void Draw(SpriteBatch batch, Rectangle destination)
        {
            // if (Children.Count() > Size.Count)
            //     throw new ArgumentException("Children is bigger than the amount of grid items");

            var itemWidth = destination.Width / Size.Columns;
            var itemHeight = destination.Height / Size.Rows;

            Console.Log($"itemWidth: {itemWidth}, {itemHeight}");
            Console.Log($"{Size.ColumnFor(0)}");

            new DrawableGroup(
                Children.Select(
                    (child, index) =>
                        child
                            .Frame(width: itemWidth, height: itemHeight)
                            .At(x: itemWidth * Size.ColumnFor(index), y: itemHeight * Size.RowFor(index))
                )
            ).Draw(batch, destination);
        }
    }

    record HoverText(string Text, SpriteFont? Font = null) : IDraw
    {
        public void Draw(SpriteBatch batch, Rectangle destination)
        {
            IClickableMenu.drawHoverText(batch, Text, Font ?? Game1.smallFont);
        }
    }

    record TextureBox() : IDraw
    {
        [SetsRequiredMembers]
        public TextureBox(TextureSprite texture, IDraw? child = null)
            : this()
        {
            Texture = texture;
            Child = child;
        }

        public required TextureSprite Texture;
        public IEnumerable<IDraw> Children = [];
        public IDraw? Child = null;
        public Padding Padding = new();

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

            new DrawableGroup(Children).Padding(Padding).Draw(batch, destination);
            Child?.Padding(Padding).Draw(batch, destination);
        }
    }

    record StringWithScrollCenteredAt(string Title, string? placeHolderWidthText = null) : IDraw
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

    record Mouse() : IDraw
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

                var cursor =
                    Cursor >= 0 ? Cursor : ((Game1.options.snappyMenus && Game1.options.gamepadControls) ? 44 : 0);

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

    public static class ForeachGrid
    {
        public static _ForeachGrid<T> Create<T>(DrawableBuilder UI, GridSize Size, IEnumerable<T> Items) =>
            new(UI, Size, Items);
    }

    public ref struct _ForeachGrid<Element>(DrawableBuilder UI, GridSize Size, IEnumerable<Element> Items)
    {
        // public static _ForeachGrid<T> Create<T>(DrawableBuilder UI, GridSize Size, IEnumerable<T> Items) =>
        //     new(UI, Size, Items);

        // public IEnumerator<T> GetEnumerator()
        // {
        //     var index = 0;
        //     var size = Size;
        //     foreach (var item in Items)
        //     {
        //         var itemWidth = (1f / Size.Columns).Pc;
        //         var itemHeight = (1f / Size.Rows).Pc;
        //         var x = itemWidth * Size.ColumnFor(index);
        //         var y = itemHeight * Size.RowFor(index);

        //         using (UI.Group(group => group.Frame(x: x, y: y, width: itemWidth, height: itemHeight)))
        //         {
        //             yield return item;
        //         }

        //         index += 1;
        //     }
        // }

        public ForeachGridEnumerator GetEnumerator() => new ForeachGridEnumerator(UI, Items, Size);

        public ref struct ForeachGridEnumerator(DrawableBuilder UI, IEnumerable<Element> items, GridSize size)
        {
            private int index = -1;
            private IEnumerator<Element> enumerator = items.GetEnumerator();
            private ActionDisposableStruct _currentGroup = new(); // Assuming UI.Group returns IDisposable

            public Element Current => enumerator.Current;

            public bool MoveNext()
            {
                index++;
                if (enumerator.MoveNext() is false)
                    return false;

                // Dispose the group from the previous iteration

                var itemWidth = (1f / size.Columns).Pc;
                var itemHeight = (1f / size.Rows).Pc;
                var x = itemWidth * size.ColumnFor(index);
                var y = itemHeight * size.RowFor(index);

                // Open the scope for the current iteration
                _currentGroup.Dispose();
                _currentGroup = UI.Group(group => group.Frame(x: x, y: y, width: itemWidth, height: itemHeight));

                return true;
            }

            // Duck-typed Dispose called automatically by 'foreach'
            public void Dispose() => _currentGroup.Dispose();
        }
    }

    TextureSprite ItemRowBackground =>
        VisualTheme.ItemRowBackgroundTexture.Clip(VisualTheme.ItemRowBackgroundSourceRect);

    TextureSprite ScrollUpSprite => VisualTheme.ScrollUpTexture.Clip(VisualTheme.ScrollUpSourceRect);
    TextureSprite ScrollDownSprite => VisualTheme.ScrollDownTexture.Clip(VisualTheme.ScrollDownSourceRect);
    TextureSprite ScrollBarFrontSprite => VisualTheme.ScrollBarFrontTexture.Clip(VisualTheme.ScrollBarFrontSourceRect);
    TextureSprite ScrollBarBackSprite => VisualTheme.ScrollBarBackTexture.Clip(VisualTheme.ScrollBarBackSourceRect);
    TextureSprite MouseCursorOrSomethingSprite = Game1.mouseCursors.Clip(new Rectangle(384, 373, 18, 18));

    public void Render(DrawableBuilder UI)
    {
        var topLeft = Utility.getTopLeftPositionForCenteringOnScreen(base.width, base.height);
        var Bounds = new Rectangle((int)topLeft.X, (int)topLeft.Y, base.width, base.height);

        UI += Game1.fadeToBlackRect.MultiplyColor(Color.Black * 0.75f);
        using (UI.Group(x => x.Frame(Bounds)))
        {
            UI += new StringWithScrollCenteredAt(_title).At((1f / 2).Pc, -64);

            var menuPadding = new Padding(all: 16) { Top = 20, Right = 12 } + new Padding(all: 16);
            using (UI.Group(x => new TextureBox(MouseCursorOrSomethingSprite, x.Padding(menuPadding))))
            {
                // using (UI.Group(x => new Grid(gridSize, x.Drawables)))
                // {
                //     foreach (var (button, item) in elementsOnScreen)
                //     {
                //         using (UI.Group())
                //         {
                //             UI += new TextureBox()
                //             {
                //                 Texture = ItemRowBackground,
                //                 Color =
                //                     (this.hovered == item && !scrolling)
                //                         ? VisualTheme.ItemRowBackgroundHoverColor
                //                         : Color.White,
                //                 DrawShadow = false,
                //             };
                //             UI += item.Padding(new Padding(all: 12));
                //         }
                //     }
                // }

                foreach (var (button, item) in ForeachGrid.Create(UI, gridSize, elementsOnScreen))
                {
                    UI += new TextureBox()
                    {
                        Texture = ItemRowBackground,
                        Color =
                            (this.hovered == item && !scrolling)
                                ? VisualTheme.ItemRowBackgroundHoverColor
                                : Color.White,
                        DrawShadow = false,
                    };
                    UI += item.Padding(new Padding(all: 12));
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
                using (UI.Group(x => x.Frame(x: 1f.Pc, y: 0, width: 48, height: 1f.Pc).Padding(all: 4)))
                {
                    UI += ScrollUpSprite.Frame(x: 0, y: 0, width: 44, height: 48);
                    using (UI.Group(x => x.Padding(top: 48 + 12, bottom: 48 + 12).Centered(24)))
                    {
                        UI += new TextureBox(ScrollBarBackSprite);
                        UI += ScrollBarFrontSprite.Frame(24, 40).At(0, scrollbarY);
                    }
                    UI += ScrollDownSprite.Frame(x: 0, y: 1f.Pc - 48, width: 44, height: 48);
                }
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
            var UI = new DrawableBuilder();
            UI += new DrawableGroup(RenderSignal.Value);
            new DrawableGroup(UI.Finish()).Draw(batch, Game1.graphics.GraphicsDevice.Viewport.Bounds);
        }
    }
}
