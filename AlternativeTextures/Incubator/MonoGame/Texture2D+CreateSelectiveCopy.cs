using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Incubator.MonoGame;

public static class TextureExtension
{
    public static Texture2D CreateSelectiveCopy(
        this Texture2D sourceTexture,
        GraphicsDevice device,
        Rectangle selectionRect
    )
    {
        var selectiveTexture = new Texture2D(device, selectionRect.Width, selectionRect.Height);

        var dimensions = selectionRect.Width * selectionRect.Height;
        Color[] data = new Color[dimensions];

        sourceTexture.GetData(0, selectionRect, data, 0, dimensions);
        selectiveTexture.SetData(data);

        return selectiveTexture;
    }
}
