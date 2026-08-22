using System.Collections.Generic;
using System.Drawing;

namespace DralGeometry;

public record GridSize(int rows, int columns)
{
    public int Rows { get; init; } = rows;
    public int Columns { get; init; } = columns;

    public int Count
    {
        get { return Rows * Columns; }
    }

    public int ColumnFor(int index)
    {
        if (index >= Count)
            throw new ArgumentException("Higher index than possible in the GridSize");

        return index % Columns;
    }

    public int RowFor(int index)
    {
        if (index >= Count)
            throw new ArgumentException("Higher index than possible in the GridSize");

        return index / Columns;
    }

    public (int Row, int Column) PositionForIndex(int index)
    {
        if (index >= Count)
            throw new ArgumentException("Higher index than possible in the GridSize");

        return (index / Columns, index % Columns);
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
