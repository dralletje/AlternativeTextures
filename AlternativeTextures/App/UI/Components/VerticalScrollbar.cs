using System;
using Incubator;
using Incubator.MonoGame;
using Incubator.MonoGame.Drawables;
using StardewValley.Menus;

namespace AlternativeTextures.App.UI;

internal record VerticalScrollbar(float Progress) : ILayout
{
    public ShopMenu.ShopCachedTheme VisualTheme { get; init; } = new(null);

    public Action? OnUpClick = null;
    public Action? OnDownClick = null;
    public Action? OnThumbClick = null;
    public Action<FocusElement, Direction>? OnUnhandledMove = null;
    public IState<FocusElement?>? Focus = null;

    public enum FocusElement
    {
        Thumb,
        UpArrow,
        DownArrow,
    }

    public void Render(ref DrawableBuilder UI)
    {
        var currentFocus = Focus?.Value;
        using (UI.Group(Geometry.Centered(UI.Frame, 44)))
        {
            using (UI.Group(new(x: UI.Frame.X, y: UI.Frame.Y, width: 44, height: 48)))
            {
                UI += VisualTheme.ScrollUpSprite;

                UI += new OnEventsLayout()
                {
                    OnLeftClick = () =>
                    {
                        OnUpClick?.Invoke();
                        return true;
                    },
                };

                UI += new FocusHandlerLayoutStruct<FocusElement>(Focus, FocusElement.UpArrow)
                {
                    DownNeighbour = FocusElement.Thumb,
                    OnUnhandledMove = (direction) => OnUnhandledMove?.Invoke(FocusElement.UpArrow, direction),
                };
            }

            var frame2 = UI.Frame - new Padding(top: 48 + 12, bottom: 48 + 12);
            using (UI.Group(Geometry.Centered(frame2, 24)))
            {
                UI += new TextureBox(VisualTheme.ScrollBarBackSprite);

                var moveableHeight = UI.Frame.Height - 40;
                var scrollbarY = moveableHeight * Progress;

                using (UI.Group(new(x: UI.Frame.X, y: UI.Frame.Y + (int)scrollbarY, width: 24, height: 40)))
                {
                    UI += VisualTheme.ScrollBarFrontSprite;

                    UI += new OnEventsLayout()
                    {
                        OnLeftClick = () =>
                        {
                            OnThumbClick?.Invoke();
                            return true;
                        },
                    };

                    UI += new FocusHandlerLayoutStruct<FocusElement>(Focus, FocusElement.Thumb)
                    {
                        UpNeighbour = FocusElement.UpArrow,
                        DownNeighbour = FocusElement.DownArrow,
                        OnUnhandledMove = (direction) => OnUnhandledMove?.Invoke(FocusElement.Thumb, direction),
                    };
                }
            }

            using (UI.Group(new(x: UI.Frame.X, y: UI.Frame.Y + UI.Frame.Height - 48, width: 44, height: 48)))
            {
                UI += VisualTheme.ScrollDownSprite;

                UI += new OnEventsLayout()
                {
                    OnLeftClick = () =>
                    {
                        OnDownClick?.Invoke();
                        return true;
                    },
                };

                UI += new FocusHandlerLayoutStruct<FocusElement>(Focus, FocusElement.DownArrow)
                {
                    UpNeighbour = FocusElement.Thumb,
                    OnUnhandledMove = (direction) => OnUnhandledMove?.Invoke(FocusElement.DownArrow, direction),
                };
            }
        }
    }
}
