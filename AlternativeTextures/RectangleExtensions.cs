using DralGeometry;
using Microsoft.Xna.Framework;

namespace AlternativeTextures;

public static class PaddingExtensions
{
    extension(Rectangle rectangle)
    {
        public static Rectangle operator +(Rectangle rect, Padding padding) =>
            (rect.ToSystemRectangle() + padding).ToXnaRectangle();

        public static Rectangle operator -(Rectangle rect, Padding padding) => rect + (padding * -1);
    }
}

public static class RectangleExtensions
{
    extension(Rectangle rectangle)
    {
        public System.Drawing.Rectangle ToSystemRectangle() =>
            new(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

        public Rectangle FitInside(Rectangle fitting)
        {
            var containerRatio = (float)rectangle.Width / rectangle.Height;
            var fittingRatio = (float)fitting.Width / fitting.Height;

            var width = fittingRatio > containerRatio ? rectangle.Width : rectangle.Height * fittingRatio;
            var height = fittingRatio > containerRatio ? rectangle.Width / fittingRatio : rectangle.Height;

            var x = rectangle.X + (rectangle.Width - width) / 2f;
            var y = rectangle.Y + (rectangle.Height - height) / 2f;

            return new Rectangle((int)x, (int)y, (int)width, (int)height);
        }
    }

    extension(System.Drawing.Rectangle rectangle)
    {
        public Rectangle ToXnaRectangle() => new(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }
}
