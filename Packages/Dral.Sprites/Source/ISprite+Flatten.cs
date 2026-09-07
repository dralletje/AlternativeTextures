
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dral.Sprites;

file static class Renderer
{
    public static Texture2D Render(GraphicsDevice gd, int width, int height, Action<SpriteBatch> render)
    {
        var renderTarget = new RenderTarget2D(gd, width, height);
        gd.SetRenderTarget(renderTarget);
        gd.Clear(Color.Transparent);

        using (var batch = new SpriteBatch(gd))
        {
            render(batch);
        }

        gd.SetRenderTarget(null);
        return renderTarget;
    }
}


public static class ITextureExtensions
{
    extension(ISprite sprite)
    {
        public Texture2D Flatten(GraphicsDevice graphicsDevice) =>
            Renderer.Render(
                graphicsDevice,
                sprite.Width,
                sprite.Height,
                (batch) =>
                {
                    var rectangle = new Rectangle(0, 0, sprite.Width, sprite.Height);
                    batch.Begin();
                    sprite.Draw(batch, rectangle);
                    batch.End();
                }
            );
    }
}
