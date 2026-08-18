using System.Collections.Generic;
using System.Drawing;

namespace DralGeometry;

public readonly struct GridSize(int rows, int columns)
{
    public readonly int Rows { get; init; } = rows;
    public readonly int Columns { get; init; } = columns;

    public int Count
    {
        get { return Rows * Columns; }
    }
}

public static class GridExtensions
{
    extension(Rectangle rectangle)
    {
        public IEnumerable<Rectangle> SliceToGrid(GridSize gridSize)
        {
            var sliceWidth = rectangle.Width / gridSize.Columns;
            var sliceHeight = rectangle.Height / gridSize.Rows;
            return Enumerable
                .Range(0, gridSize.Rows)
                .SelectMany(row =>
                    Enumerable
                        .Range(0, gridSize.Columns)
                        .Select(column => new Rectangle(
                            rectangle.X + (sliceWidth * column),
                            rectangle.Y + (sliceHeight * row),
                            sliceWidth,
                            sliceHeight
                        ))
                );
        }
    }
}
