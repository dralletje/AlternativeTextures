using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Incubator.MonoGame.FlexibleTextures;

public record SubTexture(ITexture BaseTexture, Rectangle BaseSource) : ITexture
{
    public SubTexture(Texture2D texture, Rectangle rectangle)
        : this(new IdentityTexture(texture), rectangle) { }

    public int Width => BaseSource.Width;
    public int Height => BaseSource.Height;

    public bool IsDisposed => BaseTexture.IsDisposed;

    public void Dispose()
    {
        BaseTexture.Dispose();
        GC.SuppressFinalize(this);
    }

    private Rectangle Combine(Rectangle? src) =>
        src is { } s ? new(BaseSource.X + s.X, BaseSource.Y + s.Y, s.Width, s.Height) : BaseSource;

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
    ) => BaseTexture.Draw(sb, pos, Combine(src), col, rot, org, scale, eff, depth);

    public void Draw(
        SpriteBatch sb,
        Rectangle dest,
        Rectangle? src,
        Color col,
        float rot,
        Vector2 org,
        SpriteEffects eff,
        float depth
    ) => BaseTexture.Draw(sb, dest, Combine(src), col, rot, org, eff, depth);
}

public static class ITexture_SubTexture
{
    extension(ITexture texture)
    {
        public ITexture SubSelection(Rectangle sourceRectangle) => new SubTexture(texture, sourceRectangle);
    }
}
