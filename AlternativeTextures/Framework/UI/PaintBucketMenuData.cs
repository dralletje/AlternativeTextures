using System;
using System.Collections.Generic;
using System.Linq;
using AlternativeTextures.Framework.Models;
using ConsoleLog;
using DralGeometry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Internal;
using StardewValley.Objects;

namespace AlternativeTextures.Framework.UI;

using TextureType = TextureType;

// interface IActionable
// {
//     void Do();
// }

record TextureGridMenuItem : GridMenu.Item
{
    public required IPaintable Paintable { get; init; }
    public required TextureIdentifier TextureIdentifier { get; init; }
    public string? DisplayName { get; init; }

    public void Draw(SpriteBatch batch, Rectangle destinationRect)
    {
        if (Paintable.PreviewTexture(TextureIdentifier) is { } drawableTexture)
        {
            if (drawableTexture.Texture == null)
            {
                Console.Log($"Texture == null: {TextureIdentifier}");
                return;
            }

            var littleInset =
                destinationRect
                - new Padding()
                {
                    Top = 4,
                    Bottom = 0,
                    Left = 8,
                    Right = 8,
                };
            var rectangle = littleInset.FitInside(drawableTexture.SourceRect);
            batch.Draw(
                drawableTexture.Texture,
                rectangle,
                drawableTexture.SourceRect,
                Color.White,
                0f,
                new Vector2(0, 0),
                SpriteEffects.None,
                0f
            );
        }
    }
}

record TextureInfo
{
    public required TextureIdentifier TextureIdentifier { get; init; }
    public string? DisplayName { get; init; }
}

static class PaintBucketMenuData
{
    public static IEnumerable<TextureInfo> VanillaFloorDecorations()
    {
        var allDecorations = ItemQueryResolver.TryResolve("ALL_ITEMS (FL)", context: null);
        foreach (var decoration in allDecorations)
        {
            if (decoration.Item is Wallpaper floor)
            {
                if (!String.IsNullOrEmpty(floor.setId.Value))
                {
                    continue;
                }

                yield return new TextureInfo()
                {
                    TextureIdentifier = new()
                    {
                        Owner = AlternativeTextures.DEFAULT_OWNER,
                        Variation = floor.ParentSheetIndex,
                        Name = AlternativeTextures.DEFAULT_OWNER,
                    },
                };
            }
        }
    }

    public static IEnumerable<TextureInfo> VanillaWallpaperDecorations()
    {
        var allDecorations = ItemQueryResolver.TryResolve("ALL_ITEMS (WP)", context: null);
        foreach (var decoration in allDecorations)
        {
            if (decoration.Item is Wallpaper wallpaper)
            {
                if (!String.IsNullOrEmpty(wallpaper.setId.Value))
                {
                    continue;
                }

                yield return new TextureInfo()
                {
                    TextureIdentifier = new()
                    {
                        Owner = AlternativeTextures.DEFAULT_OWNER,
                        Variation = wallpaper.ParentSheetIndex,
                        Name = AlternativeTextures.DEFAULT_OWNER,
                    },
                };
            }
        }
    }

    public static IEnumerable<TextureInfo> VanillaTexturesFor(ModelIdentifier modelIdentifier)
    {
        switch (modelIdentifier)
        {
            case { Type: TextureType.Decoration, IsName: true, String: "Floor" }:
                foreach (var thing in VanillaFloorDecorations())
                {
                    yield return thing;
                }
                break;

            case { Type: TextureType.Decoration, IsName: true, String: "Wallpaper" }:
                foreach (var thing in VanillaWallpaperDecorations())
                {
                    yield return thing;
                }
                break;
            default:
                yield return new TextureInfo()
                {
                    TextureIdentifier = TextureIdentifier.Default,
                    DisplayName = "Default",
                };
                break;
        }
    }

    public static IEnumerable<TextureInfo> GetTexturesFor(ModelIdentifier modelIdentifier)
    {
        var availableModels = AlternativeTextures.textureManager.GetTexturesForModel(
            modelIdentifier,
            Game1.GetSeasonForLocation(Game1.currentLocation)
        );
        foreach (var model in availableModels)
        {
            yield return new() { TextureIdentifier = model.TextureIdentifier, DisplayName = model.DisplayName };
        }
    }
}
