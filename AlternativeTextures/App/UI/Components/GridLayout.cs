using System;
using System.Collections.Generic;
using DralGeometry;
using Dral.Sprites;
using Incubator.MonoGame;

namespace AlternativeTextures.App.UI;

public static class GridLayout
{
    public static Implementation<T> Create<T>(DrawableBuilder UI, GridSize Size, IEnumerable<T> Items) =>
        new(UI, Size, Items);

    public ref struct Implementation<Element>(DrawableBuilder UI, GridSize Size, IEnumerable<Element> Items)
    {
        // public static _ForeachGrid<T> Create<T>(DrawableBuilder UI, GridSize Size, IEnumerable<T> Items) =>
        //     new(UI, Size, Items);

        private DrawableBuilder _ui = UI;

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

        public Enumerator GetEnumerator() => new Enumerator(_ui, Items, Size);

        public ref struct Enumerator(DrawableBuilder UI, IEnumerable<Element> items, GridSize size)
        {
            private DrawableBuilder _UI = UI;

            private int index = -1;
            private IEnumerator<Element> enumerator = items.GetEnumerator();
            private ActionDisposableStruct _currentGroup = new(); // Assuming UI.Group returns IDisposable

            public ((int column, int row), Element) Current =>
                ((size.ColumnFor(index), size.RowFor(index)), enumerator.Current);

            public bool MoveNext()
            {
                if (enumerator.MoveNext() is false)
                    return false;

                index++;
                // Dispose the group from the previous iteration
                _currentGroup.Dispose();

                var itemWidth = _UI.Frame.Width / size.Columns;
                var itemHeight = _UI.Frame.Height / size.Rows;
                var x = _UI.Frame.X + (itemWidth * size.ColumnFor(index));
                var y = _UI.Frame.Y + (itemHeight * size.RowFor(index));

                // Open the scope for the current iteration
                _currentGroup = _UI.Group(new(x: x, y: y, width: itemWidth, height: itemHeight));

                return true;
            }

            // Duck-typed Dispose called automatically by 'foreach'
            public void Dispose() => _currentGroup.Dispose();
        }
    }
}
