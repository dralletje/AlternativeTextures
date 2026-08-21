using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Incubator;

public interface ITexture : IDisposable
{
    public void Draw(
        SpriteBatch spritebatch,
        Vector2 position,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        SpriteEffects effects,
        float layerDepth
    );

    /// Has destinationRectangle, so no scale
    public void Draw(
        SpriteBatch spritebatch,
        Rectangle destinationRectangle,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        SpriteEffects effects,
        float layerDepth
    );

    public int Height { get; }
    public int Width { get; }

    public bool IsDisposed { get; }
}

static class ITextureExtensions
{
    extension(ITexture texture)
    {
        public Texture2D Flatten(GraphicsDevice graphicsDevice) =>
            Renderer.Render(
                graphicsDevice,
                texture.Width,
                texture.Height,
                (batch) =>
                {
                    var rectangle = new Rectangle(0, 0, texture.Width, texture.Height);
                    batch.Begin();
                    batch.Draw(texture, rectangle, rectangle, Color.White);
                    batch.End();
                }
            );
    }
}

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

public record SubTexture(ITexture BaseTexture, Rectangle BaseSource) : ITexture
{
    public SubTexture(Texture2D texture, Rectangle rectangle)
        : this(texture.ITexture(), rectangle) { }

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

public record OverlayTexture(List<ITexture> Layers) : ITexture
{
    public int Width => Layers[0].Width;
    public int Height => Layers[0].Height;

    public bool IsDisposed => Layers.Any(x => x.IsDisposed);

    public void Dispose()
    {
        foreach (var layer in Layers)
        {
            layer.Dispose();
        }
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
    )
    {
        foreach (var layer in Layers)
            layer.Draw(sb, pos, src, col, rot, org, scale, eff, depth);
    }

    public void Draw(
        SpriteBatch sb,
        Rectangle dest,
        Rectangle? src,
        Color col,
        float rot,
        Vector2 org,
        SpriteEffects eff,
        float depth
    )
    {
        foreach (var layer in Layers)
            layer.Draw(sb, dest, src, col, rot, org, eff, depth);
    }
}

static class MonoGameExtensions
{
    extension(SpriteBatch spritebatch)
    {
        /// Has destination position and scale arguments
        public void Draw(
            ITexture texture,
            Vector2 position,
            Rectangle? sourceRectangle,
            Color color,
            float rotation,
            Vector2 origin,
            float scale,
            SpriteEffects effects,
            float layerDepth
        )
        {
            spritebatch.Draw(
                texture,
                position,
                sourceRectangle,
                color,
                rotation,
                origin,
                new Vector2(scale, scale),
                effects,
                layerDepth
            );
        }

        public void Draw(
            ITexture texture,
            Vector2 position,
            Rectangle? sourceRectangle,
            Color color,
            float rotation,
            Vector2 origin,
            Vector2 scale,
            SpriteEffects effects,
            float layerDepth
        )
        {
            texture.Draw(spritebatch, position, sourceRectangle, color, rotation, origin, scale, effects, layerDepth);
        }

        /// Has destinationRectangle, so no scale
        public void Draw(
            ITexture texture,
            Rectangle destinationRectangle,
            Rectangle? sourceRectangle,
            Color color,
            float rotation,
            Vector2 origin,
            SpriteEffects effects,
            float layerDepth
        )
        {
            texture.Draw(
                spritebatch,
                destinationRectangle,
                sourceRectangle,
                color,
                rotation,
                origin,
                effects,
                layerDepth
            );
        }

        public void Draw(ITexture texture, Rectangle destinationRectangle, Rectangle sourceRectangle, Color color)
        {
            spritebatch.Draw(
                texture,
                destinationRectangle,
                sourceRectangle,
                color,
                rotation: 0f,
                origin: Vector2.Zero,
                effects: SpriteEffects.None,
                layerDepth: 0f
            );
        }
    }

    extension(Texture2D texture)
    {
        public ITexture ITexture() => new IdentityTexture(texture);

        public Texture2D CreateSelectiveCopyCPU(GraphicsDevice device, Rectangle selectionRect)
        {
            var extractPixels = new Color[selectionRect.Width * selectionRect.Height];

            if (texture.Bounds.Contains(selectionRect) is false)
                throw new ArgumentException("selectionRect is not fully inside actual texture bounds");

            // Get the required pixels
            texture.GetData(0, selectionRect, extractPixels, 0, extractPixels.Length);

            // Set the required pixels
            var extractedTexture = new Texture2D(device, selectionRect.Width, selectionRect.Height);
            extractedTexture.SetData(extractPixels);
            return extractedTexture;
        }

        public Texture2D CreateSelectiveCopy(GraphicsDevice gd, Rectangle selectionRect)
        {
            return Renderer.Render(
                gd,
                selectionRect.Width,
                selectionRect.Height,
                batch =>
                {
                    batch.Begin(samplerState: SamplerState.PointClamp);
                    batch.Draw(
                        texture,
                        new Rectangle(0, 0, selectionRect.Width, selectionRect.Height),
                        selectionRect,
                        Color.White
                    );
                    batch.End();
                }
            );
        }
    }
}

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
