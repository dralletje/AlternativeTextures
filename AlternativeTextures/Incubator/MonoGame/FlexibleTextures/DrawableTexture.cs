using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Incubator.MonoGame.FlexibleTextures;

public record DrawableTexture() : ITexture
{
    public required Texture2D Texture { get; init; }
    public required Rectangle SourceRect { get; init; }

    public int Height => SourceRect.Height;
    public int Width => SourceRect.Width;

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
    ) => new SubTexture(new IdentityTexture(Texture), SourceRect).Draw(sb, pos, src, col, rot, org, scale, eff, depth);

    public void Draw(
        SpriteBatch sb,
        Rectangle dest,
        Rectangle? src,
        Color col,
        float rot,
        Vector2 org,
        SpriteEffects eff,
        float depth
    ) => new SubTexture(new IdentityTexture(Texture), SourceRect).Draw(sb, dest, src, col, rot, org, eff, depth);
}
