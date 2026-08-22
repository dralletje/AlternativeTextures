using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Incubator.MonoGame.FlexibleTextures;

public record IdentityTexture(Texture2D Texture) : ITexture
{
    public int Width => Texture.Width;
    public int Height => Texture.Height;

    public bool IsDisposed => Texture.IsDisposed;

    public void Dispose()
    {
        Texture.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Draw(
        SpriteBatch sb,
        Vector2 pos,
        Rectangle? src,
        Color col,
        float rot,
        Vector2 org,
        Vector2 scale,
        SpriteEffects eff,
        float depth
    ) => sb.Draw(Texture, pos, src, col, rot, org, scale, eff, depth);

    public void Draw(
        SpriteBatch sb,
        Rectangle dest,
        Rectangle? src,
        Color col,
        float rot,
        Vector2 org,
        SpriteEffects eff,
        float depth
    ) => sb.Draw(Texture, dest, src, col, rot, org, eff, depth);
}
