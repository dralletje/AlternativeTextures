using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Incubator.MonoGame;

static class Renderer
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
