using Microsoft.Xna.Framework;

namespace Incubator.MonoGame;

static class Rectangle_Layout
{
    extension(Rectangle rectangle)
    {
        public static Rectangle CenteredInside(Rectangle container, int? width = null, int? height = null)
        {
            var widthNotNull = width ?? container.Width;
            var heightNotNull = height ?? container.Height;

            var spareSpaceWidth = container.Width - widthNotNull;
            var spareSpaceHeight = container.Height - heightNotNull;

            return new(
                container.Left + (spareSpaceWidth / 2),
                container.Top + (spareSpaceHeight / 2),
                container.Width - spareSpaceWidth,
                container.Height - spareSpaceHeight
            );
        }
    }
}
