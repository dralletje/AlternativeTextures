using System;
using System.Collections.Generic;
using AlternativeTextures.Framework;
using AlternativeTextures.Framework.Paintable;
using DralGeometry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Internal;
using StardewValley.Objects;

namespace AlternativeTextures.App.UI;

// interface IActionable
// {
//     void Do();
// }

record TextureGridMenuItem : GridMenu.Item
{
    public required IPaintable Paintable { get; init; }
    public required WorldObject Related { get; init; }
    public required TextureIdentifierWithoutSeason TextureIdentifier { get; init; }
    public string? DisplayName { get; init; }

    public void Draw(SpriteBatch batch, Rectangle destinationRect)
    {
        if (
            DrawPaintable.PreviewTexture(TextureIdentifier.WithSeason(Game1.currentLocation.GetSeason()), Related) is
            { } drawableTexture
        )
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
    public required TextureIdentifierWithoutSeason TextureIdentifier { get; init; }
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
                if (!string.IsNullOrEmpty(floor.setId.Value))
                    continue;

                yield return new TextureInfo()
                {
                    TextureIdentifier = new(
                        AlternativeTextures.DEFAULT_OWNER,
                        ModelIdentifier.Floor,
                        floor.ParentSheetIndex
                    ),
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
                if (!string.IsNullOrEmpty(wallpaper.setId.Value))
                    continue;

                yield return new TextureInfo()
                {
                    TextureIdentifier = new(
                        AlternativeTextures.DEFAULT_OWNER,
                        ModelIdentifier.Wallpaper,
                        wallpaper.ParentSheetIndex
                    ),
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
                    yield return thing;
                break;

            case { Type: TextureType.Decoration, IsName: true, String: "Wallpaper" }:
                foreach (var thing in VanillaWallpaperDecorations())
                    yield return thing;
                break;

            default:
                yield return new TextureInfo()
                {
                    TextureIdentifier = TextureIdentifierWithoutSeason.DefaultFor(modelIdentifier),
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
            yield return new()
            {
                TextureIdentifier = model.UniqueIdentifierWithoutSeason,
                DisplayName = model.DisplayName,
            };
        }
    }
}
