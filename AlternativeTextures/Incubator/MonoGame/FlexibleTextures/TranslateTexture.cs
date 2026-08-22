using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Incubator.MonoGame.FlexibleTextures;

public record TranslatedTexture(ITexture BaseTexture, Vector2 Offset) : ITexture
{
    public int Width => BaseTexture.Width + (int)Offset.X;
    public int Height => BaseTexture.Height + (int)Offset.Y;

    public bool IsDisposed => BaseTexture.IsDisposed;

    public void Dispose()
    {
        BaseTexture.Dispose();
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
    ) => BaseTexture.Draw(sb, pos + Offset, src, col, rot, org, scale, eff, depth);

    public void Draw(
        SpriteBatch sb,
        Rectangle dest,
        Rectangle? src,
        Color col,
        float rot,
        Vector2 org,
        SpriteEffects eff,
        float depth
    ) =>
        BaseTexture.Draw(
            sb,
            new(dest.X + (int)Offset.X, dest.Y + (int)Offset.Y, dest.Width, dest.Height),
            src,
            col,
            rot,
            org,
            eff,
            depth
        );
}

public static class ITexture_TranslatedTexture
{
    extension(ITexture texture)
    {
        public ITexture Translate(Vector2 offset) => new TranslatedTexture(texture, offset);
    }
}
