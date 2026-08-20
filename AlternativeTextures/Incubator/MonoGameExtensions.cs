using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Incubator;

public record DrawableTexture()
{
    public required Texture2D Texture { get; init; }
    public required Rectangle SourceRect { get; init; }

    [SetsRequiredMembers]
    public DrawableTexture(Texture2D texture)
        : this()
    {
        Texture = texture;
        SourceRect = new()
        {
            X = 0,
            Y = 0,
            Width = texture.Width,
            Height = texture.Height,
        };
    }

    [SetsRequiredMembers]
    public DrawableTexture(DrawableTexture parent, Rectangle sourceRect)
        : this()
    {
        Texture = parent.Texture;
        SourceRect = new()
        {
            X = parent.SourceRect.X + sourceRect.X,
            Y = parent.SourceRect.Y + sourceRect.Y,
            /// TODO Make sure the sourceRect fits inside the parent SourceRect
            Width = sourceRect.Width,
            Height = sourceRect.Height,
        };
    }

    public DrawableTexture WithSourceRect(Rectangle sourceRect)
    {
        return new DrawableTexture(this, sourceRect);
    }

    public Texture2D Flatten(GraphicsDevice graphicsDevice) => Texture.CreateSelectiveCopy(graphicsDevice, SourceRect);
}

static class MonoGameExtensions
{
    extension(SpriteBatch spritebatch)
    {
        public void Draw(
            DrawableTexture texture,
            Rectangle destination,
            Color? overlayColor = null,
            float rotation = 0f,
            Vector2? origin = null,
            SpriteEffects? effects = null,
            float layerDepth = 1f
        )
        {
            spritebatch.Draw(
                texture.Texture,
                destination,
                texture.SourceRect,
                overlayColor ?? Color.White,
                rotation,
                origin ?? Vector2.Zero,
                effects ?? SpriteEffects.None,
                layerDepth
            );
        }

        public void Draw(
            DrawableTexture texture,
            Vector2 position,
            Color color,
            float rotation,
            Vector2 origin,
            Vector2 scale,
            SpriteEffects effects,
            float layerDepth
        ) { }

        public void Draw(
            DrawableTexture texture,
            Vector2 position,
            Color color,
            float rotation,
            Vector2 origin,
            float scale,
            SpriteEffects effects,
            float layerDepth
        ) { }

        public void Draw(
            Texture2D texture,
            Vector2 position,
            Color color,
            float rotation,
            Vector2 origin,
            float scale,
            SpriteEffects effects,
            float layerDepth
        ) { }
    }

    extension(Texture2D texture)
    {
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
