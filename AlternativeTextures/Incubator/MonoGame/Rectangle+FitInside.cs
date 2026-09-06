using Microsoft.Xna.Framework;

namespace AlternativeTextures;

public static class RectangleExtensions
{
    extension(Rectangle rectangle)
    {
        public Rectangle FitInside(Rectangle fitting)
        {
            var containerRatio = (float)rectangle.Width / rectangle.Height;
            var fittingRatio = (float)fitting.Width / fitting.Height;

            var width = fittingRatio > containerRatio ? rectangle.Width : rectangle.Height * fittingRatio;
            var height = fittingRatio > containerRatio ? rectangle.Width / fittingRatio : rectangle.Height;

            var x = rectangle.X + ((rectangle.Width - width) / 2f);
            var y = rectangle.Y + ((rectangle.Height - height) / 2f);

            return new Rectangle((int)x, (int)y, (int)width, (int)height);
        }
    }
}
