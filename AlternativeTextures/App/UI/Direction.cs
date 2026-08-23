using System;

namespace AlternativeTextures.App.UI;

public enum Direction
{
    Up,
    Down,
    Left,
    Right,
}

public static class Direction_FromNumber
{
    extension(Direction direction)
    {
        public static Direction FromNumber(int directionInt) =>
            directionInt switch
            {
                0 => Direction.Up,
                1 => Direction.Right,
                2 => Direction.Down,
                3 => Direction.Left,
                _ => throw new ArgumentException($"Odd direction {directionInt}"),
            };
    }
}
