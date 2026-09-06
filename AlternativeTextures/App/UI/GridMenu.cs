using System;
using System.Collections.Generic;
using System.Linq;
using Dral.Optics;
using DralGeometry;
using Incubator;
using Incubator.MonoGame;
using Dral.Sprites;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Menus;

namespace AlternativeTextures.App.UI;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class GenerateLensesAttribute : Attribute { }

internal class GridMenu(
    ICollection<GridMenu.Item> items,
    GridSize gridSize,
    string uiTitle = "Paint Bucket",
    Action<GridMenu.Item>? onPress = null
) : LayoutMenu
{
    internal interface Item : IDraw
    {
        public string? DisplayName { get; init; }
        public string? HoverText { get; init; }
    }

    readonly State<FocusElement?> Focus = new(new FocusElement.InScrollbar(VerticalScrollbar.FocusElement.UpArrow));
    readonly State<bool> IsScrolling = new(false);
    readonly State<Item?> hovered = new(null);
    // readonly State<int> rowsScrolled = new(0);

    ShopMenu.ShopCachedTheme VisualTheme = new(null);

    int VirtualRows => (int)Math.Ceiling((double)items.Count / gridSize.Columns);
    int MaxScroll => (int)Math.Max(0, Math.Ceiling((double)items.Count / gridSize.Columns) - gridSize.Rows);

    public IState<int> rowsScrolled => field ??= new MappedState<int, int>(
        new State<int>(0),
        v => Math.Clamp(v, 0, this.MaxScroll),
        v => Math.Clamp(v, 0, this.MaxScroll)
    );

    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);
        if (direction > 1)
        {
            rowsScrolled.Value--;
        }
        else if (direction < 3)
        {
            rowsScrolled.Value++;
        }
    }

    public override void Render(ref DrawableBuilder UI)
    {
        var menuWidth = 832;
        var menuHeight = 576;
        if (
            LocalizedContentManager.CurrentLanguageCode
            is LocalizedContentManager.LanguageCode.ko
                or LocalizedContentManager.LanguageCode.fr
        )
        {
            menuHeight += 64;
        }

        // var topLeft = Utility.getTopLeftPositionForCenteringOnScreen(menuWidth, menuHeight);
        // var Bounds = new Rectangle((int)topLeft.X, (int)topLeft.Y, menuWidth, menuHeight);

        UI += Game1.fadeToBlackRect.MultiplyColor(Color.Black * 0.75f);
        using (UI.Group(Rectangle.CenteredInside(UI.Frame, width: menuWidth, height: menuHeight)))
        {
            UI += new StringWithScrollCenteredAt(uiTitle).At(UI.Frame.Width / 2, -64);

            UI += new TextureBox(MouseCursorOrSomethingSprite);

            var menuPadding = new Padding(all: 16) { Top = 20, Right = 12 } + new Padding(all: 16);
            using (UI.Group(UI.Frame - menuPadding))
            {
                var currentPageItems = items.Skip(rowsScrolled.Value * gridSize.Columns).Take(gridSize.Count);
                foreach (var (coord, item) in GridLayout.Create(UI, gridSize, currentPageItems))
                {
                    UI += new TextureBox()
                    {
                        Texture = VisualTheme.ItemRowBackgroundSprite,
                        Color =
                            (this.hovered.Value == item && !IsScrolling)
                                ? VisualTheme.ItemRowBackgroundHoverColor
                                : Color.White,
                        DrawShadow = false,
                    };

                    UI += item.Padding(new Padding(all: 12));

                    if (onPress is { } onPressYeah)
                    {
                        UI += new OnEventsLayout()
                        {
                            OnLeftClick = () =>
                            {
                                onPressYeah(item);
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
                    VirtualRows == 0 ? 0 : Math.Clamp((float)rowsScrolled.Value / (VirtualRows - gridSize.Rows), 0, 1);
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
                    // Focus = new InScrollbarLens(Focus),
                    // Focus = new PrismStateStruct<FocusElement, VerticalScrollbar.FocusElement>(
                    //     Focus,
                    //     FocusElement.InScrollbar.Prism
                    // ),
                    Focus = FocusElement.InScrollbar.Prism.ZoomStruct(Focus),
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
                                VerticalScrollbar.FocusElement.Thumb => (gridSize.Rows / 2) * (gridSize.Columns - 1),
                                VerticalScrollbar.FocusElement.DownArrow => gridSize.Count - 1,
                            };

                            var itemsInCurrentView = items.Count - (rowsScrolled.Value * gridSize.Columns);
                            startindex = Math.Clamp(startindex, 0, itemsInCurrentView - 1);
                            var coord = gridSize.PositionForIndex(startindex);
                            Focus.Value = new FocusElement.InGrid(coord);
                        }
                    },
                }.Frame(frame);
            }

            if (hovered?.Value?.DisplayName is { } displayName)
            {
                UI += new StringWithScrollCenteredAt(displayName, "").At(x: UI.Frame.Width / 2, y: UI.Frame.Height);
            }
        }

        if (this.hovered?.Value?.HoverText is { } hoverText)
        {
            UI += new HoverText(hoverText);
        }

        UI += new Mouse();
    }

    protected override void ClearHover()
    {
        this.hovered.Value = null;
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

    void snapBehavior(Direction direction, (int column, int row) coords)
    {
        var (column, row) = coords;
        var index = (row * gridSize.Columns) + row;

        var isFirstRow = row == 0;
        var isLastRow = row + 1 == gridSize.Rows;

        var isFirstColumn = column == 0;
        var isLastColumn = column + 1 == gridSize.Columns;

        var canScrollMore = rowsScrolled.Value + gridSize.Rows < VirtualRows;
        var itemsScrolled = rowsScrolled.Value * gridSize.Columns;

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
                    if (isFirstRow && rowsScrolled.Value > 0)
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

    private void downArrowPressed()
    {
        rowsScrolled.Value++;
    }

    private void upArrowPressed()
    {
        rowsScrolled.Value--;
    }

    // [Union]
    public closed record FocusElement
    {
        [Dral.Optics.Generate.Prism]
        public record InScrollbar(VerticalScrollbar.FocusElement focusElement) : FocusElement { }

        public record InGrid((int column, int row) Element) : FocusElement;
    }

    /// TODO Go over the code and collect these `Game!.mouseCursors` sprites
    /// .... because I don't know what they are, but they aint mouseCursors
    TextureSprite MouseCursorOrSomethingSprite = Game1.mouseCursors.Clip(new Rectangle(384, 373, 18, 18));
}

static class IPrismExtensions
{
    class PrismStateStruct<Outer, Inner>(IState<Outer?> state, IPrism<Outer, Inner> prism) : IState<Inner?>
        where Inner : struct
    {
        public Inner? Value
        {
            get =>
                state.Value switch
                {
                    null => default,
                    { } notnull when prism.TryDowncast(notnull, out var x) => x,
                    _ => default,
                };
            set => state.Value = value is { } notnull ? prism.Upcast(notnull) : default;
        }

        public IState<Inner?> asState() => this;
    }

    // extension<From, To>(IPrism<From, To> prism) where To: class
    // {
    //     public IState<To?> ZoomClass(IState<From?> outerState)
    //     {
    //         return new PrismStateStruct<From, To>(outerState, prism);
    //     }
    // }

    extension<From, To>(IPrism<From, To> prism)
        where To : struct
    {
        public IState<To?> ZoomStruct(IState<From?> outerState)
        {
            return new PrismStateStruct<From, To>(outerState, prism);
        }
    }
}

// ////////////////////////////////////////////////////////////

// [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
// public class GenerateLensesAttribute : Attribute { }

// public interface IPrism<Outer, Inner>
// {
//     public bool TryDowncast(Outer outer, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out Inner inner);
//     public Outer Upcast(Inner inner);
// }

// class Prism : IPrism<FocusElement, int>
// {
//     public bool TryDowncast(FocusElement outer, [MaybeNullWhen(false)] out int inner)
//     {
//         if (outer is FocusElement.InScrollbar(var x))
//         {
//             inner = x;
//             return true;
//         }
//         else
//         {
//             inner = default;
//             return false;
//         }
//     }

//     public FocusElement Upcast(int inner) => new FocusElement.InScrollbar(inner);
// }

// public delegate bool TryDowncaster<Outer, Inner>(Outer outer, [MaybeNullWhen(false)] out Inner value);

// public class Prism<Outer, Inner>(TryDowncaster<Outer, Inner> tryDowncast, Func<Inner, Outer> upcast)
//     : IPrism<Outer, Inner>
// {
//     public bool TryDowncast(Outer outer, [MaybeNullWhen(false)] out Inner inner) => tryDowncast(outer, out inner);

//     public Outer Upcast(Inner inner) => upcast(inner);
// }

// public abstract record FocusElement
// {
//     public record InScrollbar(int focusElement) : FocusElement;

//     public record InGrid((int column, int row) Element) : FocusElement;
// }

// static class OpticsExtensions
// {
//     class Prism : IPrism<FocusElement, int>
//     {
//         public bool TryDowncast(FocusElement outer, out int inner)
//         {
//             if (outer is FocusElement.InScrollbar(var x))
//             {
//                 inner = x;
//                 return true;
//             }
//             else
//             {
//                 inner = default;
//                 return false;
//             }
//         }

//         public FocusElement Upcast(int inner) => new FocusElement.InScrollbar(inner);
//     }

//     extension(FocusElement.InScrollbar target)
//     {
//         public IPrism<FocusElement, int> Lens => new Prism();
//     }
// }

public abstract record FocusElement
{
    [Dral.Optics.Generate.Prism]
    public record InScrollbarr2222444(int focusElement) : FocusElement { }

    public record InGrid((int column, int row) Element) : FocusElement;
}
