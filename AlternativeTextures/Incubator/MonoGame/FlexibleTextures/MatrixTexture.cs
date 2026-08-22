// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Microsoft.Xna.Framework;
// using Microsoft.Xna.Framework.Graphics;

// namespace Incubator.MonoGame.FlexibleTextures;

// public class MatrixTexture(ITexture baseTexture) : ITexture
// {
//     public ITexture BaseTexture { get; } = baseTexture;
//     public Matrix Transform { get; private set; } = Matrix.Identity;

//     // --- Dynamic Bounding Box ---

//     private Rectangle GetBoundingBox()
//     {
//         // 1. Define the four corners of the original texture
//         Vector2 tl = Vector2.Transform(Vector2.Zero, Transform);
//         Vector2 tr = Vector2.Transform(new Vector2(BaseTexture.Width, 0), Transform);
//         Vector2 bl = Vector2.Transform(new Vector2(0, BaseTexture.Height), Transform);
//         Vector2 br = Vector2.Transform(new Vector2(BaseTexture.Width, BaseTexture.Height), Transform);

//         // 2. Find the min and max X/Y to form the new bounding box
//         float minX = Math.Min(Math.Min(tl.X, tr.X), Math.Min(bl.X, br.X));
//         float maxX = Math.Max(Math.Max(tl.X, tr.X), Math.Max(bl.X, br.X));
//         float minY = Math.Min(Math.Min(tl.Y, tr.Y), Math.Min(bl.Y, br.Y));
//         float maxY = Math.Max(Math.Max(tl.Y, tr.Y), Math.Max(bl.Y, br.Y));

//         return new Rectangle(
//             (int)Math.Floor(minX),
//             (int)Math.Floor(minY),
//             (int)Math.Ceiling(maxX - minX),
//             (int)Math.Ceiling(maxY - minY)
//         );
//     }

//     public int Width => GetBoundingBox().Width;
//     public int Height => GetBoundingBox().Height;

//     // --- Fluent Matrix Modifiers ---

//     public MatrixTexture Translate(Vector2 offset)
//     {
//         Transform *= Matrix.CreateTranslation(new Vector3(offset, 0f));
//         return this;
//     }

//     /// <summary>
//     /// Rotates around the exact center of the base texture.
//     /// </summary>
//     public MatrixTexture Rotate(float radians)
//     {
//         Vector2 center = new Vector2(BaseTexture.Width / 2f, BaseTexture.Height / 2f);
//         return RotateAround(radians, center);
//     }

//     /// <summary>
//     /// Rotates around a specific local pixel coordinate (e.g. Vector2.Zero for top-left).
//     /// </summary>
//     public MatrixTexture RotateAround(float radians, Vector2 pivot)
//     {
//         Transform *=
//             Matrix.CreateTranslation(new Vector3(-pivot, 0f))
//             * Matrix.CreateRotationZ(radians)
//             * Matrix.CreateTranslation(new Vector3(pivot, 0f));
//         return this;
//     }

//     public MatrixTexture Scale(float scale)
//     {
//         Transform *= Matrix.CreateScale(scale);
//         return this;
//     }

//     // --- Draw Implementations ---

//     public void Draw(
//         SpriteBatch sb,
//         Vector2 pos,
//         Rectangle? src,
//         Color col,
//         float rot,
//         Vector2 org,
//         float scale,
//         SpriteEffects eff,
//         float layerDepth
//     )
//     {
//         var parentTransform =
//             Matrix.CreateTranslation(new Vector3(-org, 0f))
//             * Matrix.CreateScale(scale)
//             * Matrix.CreateRotationZ(rot)
//             * Matrix.CreateTranslation(new Vector3(pos, 0f));

//         var finalMatrix = Transform * parentTransform;

//         if (!finalMatrix.Decompose(out var outScale, out var outRot, out var outTrans))
//         throw new ArgumentException("Matrix could not be decomposed!!");

//         var finalRot = (float)(2.0 * Math.Atan2(outRot.Z, outRot.W));
//         var finalPos = new Vector2(outTrans.X, outTrans.Y);

//         BaseTexture.Draw(sb, finalPos, src, col, finalRot, Vector2.Zero, outScale.X, eff, layerDepth);
//     }

//     public void Draw(
//         SpriteBatch sb,
//         Rectangle dest,
//         Rectangle? src,
//         Color col,
//         float rot,
//         Vector2 org,
//         SpriteEffects eff,
//         float layerDepth
//     )
//     {
//         // Uses the dynamic Width/Height we calculated to properly evaluate scale
//         float sourceWidth = src?.Width ?? Width;
//         float calculatedScale = dest.Width / sourceWidth;

//         Vector2 pos = new Vector2(dest.X, dest.Y);

//         Matrix parentTransform =
//             Matrix.CreateTranslation(new Vector3(-org, 0f))
//             * Matrix.CreateScale(calculatedScale)
//             * Matrix.CreateRotationZ(rot)
//             * Matrix.CreateTranslation(new Vector3(pos, 0f));

//         Matrix finalMatrix = Transform * parentTransform;
//         finalMatrix.Decompose(out Vector3 outScale, out Quaternion outRot, out Vector3 outTrans);

//         float finalRot = (float)(2.0 * Math.Atan2(outRot.Z, outRot.W));
//         Vector2 finalPos = new Vector2(outTrans.X, outTrans.Y);

//         BaseTexture.Draw(sb, finalPos, src, col, finalRot, Vector2.Zero, outScale.X, eff, layerDepth);
//     }
// }
