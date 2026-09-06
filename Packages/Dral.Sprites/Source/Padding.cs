using System;
using Microsoft.Xna.Framework;

namespace Dral.Sprites;

public record Padding()
{
    public int Top { get; init; } = 0;
    public int Bottom { get; init; } = 0;
    public int Left { get; init; } = 0;
    public int Right { get; init; } = 0;

    public Padding(int all)
        : this()
    {
        Top = all;
        Bottom = all;
        Left = all;
        Right = all;
    }

    public Padding(int horizontal = 0, int vertical = 0)
        : this()
    {
        Top = horizontal;
        Bottom = horizontal;
        Left = vertical;
        Right = vertical;
    }

    public Padding(int left = 0, int right = 0, int top = 0, int bottom = 0)
        : this()
    {
        Top = top;
        Bottom = bottom;
        Left = left;
        Right = right;
    }

    public static Padding operator *(Padding p, int factor) =>
        new()
        {
            Top = p.Top * factor,
            Bottom = p.Bottom * factor,
            Left = p.Left * factor,
            Right = p.Right * factor,
        };

    public static Padding operator +(Padding a, Padding b) =>
        new()
        {
            Top = a.Top + b.Top,
            Bottom = a.Bottom + b.Bottom,
            Left = a.Left + b.Left,
            Right = a.Right + b.Right,
        };

    public static Padding operator -(Padding a, Padding b) => a + -b;

    public static Padding operator -(Padding a) => a * -1;
}

public static class PaddingExtensions
{
    extension(Rectangle rectangle)
    {
        public static Rectangle operator +(Rectangle rect, Padding padding)
        {
            var left = rect.Left - padding.Left;
            var right = rect.Right + padding.Right;
            var top = rect.Top - padding.Top;
            var bottom = rect.Bottom + padding.Bottom;
            return new Rectangle()
            {
                X = left > right ? (left + right) / 2 : left,
                Y = top > bottom ? (top + bottom) / 2 : top,
                Width = Math.Max(right - left, 0),
                Height = Math.Max(bottom - top, 0),
            };
        }

        public static Rectangle operator -(Rectangle rect, Padding padding) => rect + (padding * -1);
    }
}
