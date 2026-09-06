using System;
using System.Collections.Generic;
using AlternativeTextures;
using AlternativeTextures.App.UI;
using Incubator;
using Dral.Sprites;
using Microsoft.Xna.Framework;
using StardewValley;
using Incubator.MonoGame;

static class MouseHelper
{
    public static void SnapToBounds(Rectangle bounds)
    {
        Game1.setMousePosition(bounds.Right - (bounds.Width / 4), bounds.Bottom - (bounds.Height / 4), ui_scale: true);
    }
}

public static class Component_Id
{
    extension(ILayout layout)
    {
        public ILayout Focus<T>(IState<T?>? focusState, T me, MoveHandlers<T> moveHandlers)
            where T : struct, IEqualityOperators<T, T> =>
            new CombineLayout(
                layout,
                new FocusHandlerLayoutStruct<T>(FocusState: focusState, Me: me)
                {
                    UpNeighbour = moveHandlers.UpNeighbour,
                    LeftNeighbour = moveHandlers.LeftNeighbour,
                    DownNeighbour = moveHandlers.DownNeighbour,
                    RightNeighbour = moveHandlers.RightNeighbour,

                    OnUnhandledMove = moveHandlers.OnUnhandledMove,
                }
            );

        public ILayout On<T>(Func<bool>? onLeftClick) =>
            new CombineLayout(layout, new OnEventsLayout() { OnLeftClick = onLeftClick });
    }
}

public record OnEventsLayout() : ILayout
{
    public Func<bool>? OnLeftClick { get; init; } = null;

    public void Render(ref DrawableBuilder UI)
    {
        var currentFrame = UI.Frame;
        if (OnLeftClick is not null)
        {
            UI.OnLeftClick += (point) =>
            {
                if (!currentFrame.Contains(point))
                    return false;

                OnLeftClick.Invoke();
                return true;
            };
        }
    }
}

public record MoveHandlers<T>()
{
    public T? UpNeighbour;
    public T? LeftNeighbour;
    public T? DownNeighbour;
    public T? RightNeighbour;

    public Action<Direction>? OnUnhandledMove;
}

public record CombineLayout(ILayout a, ILayout b) : ILayout
{
    public void Render(ref DrawableBuilder UI)
    {
        UI += a;
        UI += b;
    }
}

public record FocusHandlerLayoutStruct<T>(IState<T?>? FocusState, T Me) : ILayout
    where T : struct
{
    public T? UpNeighbour;
    public T? LeftNeighbour;
    public T? DownNeighbour;
    public T? RightNeighbour;

    public Action<Direction>? OnUnhandledMove;

    public void Render(ref DrawableBuilder UI)
    {
        var currentFrame = UI.Frame;
        var previousFocus = FocusState?.Value;
        var previousSnappyMenu = Game2.SnappyMenus.Value;

        UI.OnUpdate += (isFirst) =>
        {
            if (!Game1.options.SnappyMenus)
                return;
            if (FocusState?.Value is not { } nextFocus)
                return;

            if (nextFocus.Equals(Me))
            {
                if (
                    isFirst
                    || !EqualityComparer<T?>.Default.Equals(previousFocus, nextFocus)
                    || previousSnappyMenu != Game2.SnappyMenus.Value
                )
                {
                    MouseHelper.SnapToBounds(currentFrame);
                }
            }
        };

        UI.OnMovementKey += (Direction direction) =>
        {
            if (EqualityComparer<T?>.Default.Equals(previousFocus, Me))
            {
                var maybeNextFocus = direction switch
                {
                    Direction.Up => UpNeighbour,
                    Direction.Right => RightNeighbour,
                    Direction.Down => DownNeighbour,
                    Direction.Left => LeftNeighbour,
                };
                if (maybeNextFocus is { } nextFocus)
                {
                    FocusState?.Value = nextFocus;
                }
                else
                {
                    OnUnhandledMove?.Invoke(direction);
                }
            }
        };
    }
}

public record FocusHandlerLayoutClass<T>(IState<T?>? FocusState, T Me) : ILayout
    where T : class
{
    public T? UpNeighbour;
    public T? LeftNeighbour;
    public T? DownNeighbour;
    public T? RightNeighbour;

    public Action<Direction>? OnUnhandledMove;

    public void Render(ref DrawableBuilder UI)
    {
        var currentFrame = UI.Frame;
        var previousFocus = FocusState?.Value;
        var previousSnappyMenu = Game2.SnappyMenus.Value;

        UI.OnUpdate += (bool isFirst) =>
        {
            if (!Game1.options.SnappyMenus)
                return;
            if (FocusState?.Value is not { } nextFocus)
                return;

            if (nextFocus.Equals(Me))
            {
                if (
                    isFirst
                    || !EqualityComparer<T>.Default.Equals(previousFocus, nextFocus)
                    || previousSnappyMenu != Game2.SnappyMenus.Value
                )
                {
                    MouseHelper.SnapToBounds(currentFrame);
                }
            }
        };

        UI.OnMovementKey += (Direction direction) =>
        {
            if (EqualityComparer<T>.Default.Equals(previousFocus, Me))
            {
                var maybeNextFocus = direction switch
                {
                    Direction.Up => UpNeighbour,
                    Direction.Right => RightNeighbour,
                    Direction.Down => DownNeighbour,
                    Direction.Left => LeftNeighbour,
                };
                if (maybeNextFocus is { } nextFocus)
                {
                    FocusState?.Value = nextFocus;
                }
                else
                {
                    OnUnhandledMove?.Invoke(direction);
                }
            }
        };
    }
}
