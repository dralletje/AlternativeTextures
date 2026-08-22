using System;
using System.Collections.Generic;
using System.Linq;
using AlternativeTextures.Framework;
using AlternativeTextures.Framework.Paintable;
using Incubator;
using Incubator.MonoGame;
using Incubator.MonoGame.FlexibleTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.GameData.Buildings;
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
    public string? HoverText { get; init; }

    public void Draw(SpriteBatch batch, Rectangle destinationRect)
    {
        if (
            DrawPaintable.PreviewTexture(TextureIdentifier.WithSeason(Game1.currentLocation.GetSeason()), Related) is
            { } drawableTexture
        )
        {
            var littleInset =
                destinationRect
                - new Padding()
                {
                    Top = 4,
                    Bottom = 0,
                    Left = 8,
                    Right = 8,
                };
            var rectangle = littleInset.FitInside(new Rectangle(0, 0, drawableTexture.Width, drawableTexture.Height));
            batch.Draw(
                drawableTexture,
                rectangle,
                new Rectangle(0, 0, drawableTexture.Width, drawableTexture.Height),
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
    public string? HoverText { get; init; }
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
                    DisplayName = $"Default #{floor.ParentSheetIndex}",
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
                    DisplayName = $"Default #{wallpaper.ParentSheetIndex}",
                };
            }
        }
    }

    public static BuildingData? GetBuildingData(ModelIdentifier modelIdentifier)
    {
        var internalBuildingType = modelIdentifier.Name switch
        {
            "Farmhouse_0" or "Farmhouse_1" or "Farmhouse_2" or "Farmhouse_3" => "Farmhouse",
            var somethingElse => somethingElse,
        };
        return Game1.buildingData.GetValueOrNull(internalBuildingType);
    }

    public static BuildingSkin? GetBuildingSkinData(BuildingData buildingData, string skinId)
    {
        return buildingData.Skins.FirstOrDefault(x => x.Id == skinId);
    }

    public static IEnumerable<TextureInfo> VanillaBuildingTestures(string buildingType)
    {
        if (GetBuildingData(TextureType.Building.WithName(buildingType)) is not { } buildingData)
            yield break;

        Console.Log($"buildingData: {buildingData}");

        yield return new()
        {
            DisplayName = "Default",
            TextureIdentifier = new(AlternativeTextures.DEFAULT_OWNER, TextureType.Building.WithName(buildingType), -1),
        };

        foreach (var skin in buildingData.Skins)
        {
            yield return new()
            {
                DisplayName = skin.Id,
                TextureIdentifier = new(skin.Id, TextureType.Building.WithName(buildingType), -1),
            };
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

            case { Type: TextureType.Building, String: var buildingType }:
                foreach (var thing in VanillaBuildingTestures(buildingType))
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
            if (model.UniqueIdentifier.IsDefault)
                continue;
            var idText = $"{model.UniqueIdentifier.Owner} #{model.UniqueIdentifier.Variation}";

            yield return new()
            {
                TextureIdentifier = model.UniqueIdentifierWithoutSeason,
                DisplayName = model.DisplayName ?? idText,
                HoverText = idText,
            };
        }
    }
}
