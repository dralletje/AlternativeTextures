// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Microsoft.Xna.Framework;
// using Microsoft.Xna.Framework.Graphics;

// namespace Incubator.MonoGame.FlexibleTextures;

// public record OverlayTexture(List<ITexture> Layers) : ITexture
// {
//     public int Width => Layers[0].Width;
//     public int Height => Layers[0].Height;

//     public bool IsDisposed => Layers.Any(x => x.IsDisposed);

//     public void Dispose()
//     {
//         foreach (var layer in Layers)
//         {
//             layer.Dispose();
//         }
//         GC.SuppressFinalize(this);
//     }

//     public void Draw(
//         SpriteBatch sb,
//         Vector2 pos,
//         Rectangle? src,
//         Color col,
//         float rot,
//         Vector2 org,
//         Vector2 scale,
//         SpriteEffects eff,
//         float depth
//     )
//     {
//         foreach (var layer in Layers)
//             layer.Draw(sb, pos, src, col, rot, org, scale, eff, depth);
//     }

//     public void Draw(
//         SpriteBatch sb,
//         Rectangle dest,
//         Rectangle? src,
//         Color col,
//         float rot,
//         Vector2 org,
//         SpriteEffects eff,
//         float depth
//     )
//     {
//         foreach (var layer in Layers)
//             layer.Draw(sb, dest, src, col, rot, org, eff, depth);
//     }
// }
