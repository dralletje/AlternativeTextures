using System;
using Incubator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;

namespace AlternativeTextures.Framework;

public record DecorationIdentifier(string? Name, int Variant)
{
    public bool IsVanilla => Name is null;

    public override string ToString() => Name is { } name ? $"{name}:{Variant}" : $"{Variant}";

    public static DecorationIdentifier? FromString(string identifier) =>
        identifier.Split(":") switch
        {
            [var name, .. var nameRest, var versionString] when int.TryParse(versionString, out var version) => new(
                $"{name}{string.Join(":", nameRest)}",
                version
            ),
            _ => null,
        };
}

public static class StardewDecorationExtensions
{
    extension(DecoratableLocation decoratableLocation)
    {
        public DecorationIdentifier? GetWallpaper(string roomId) =>
            decoratableLocation.appliedWallpaper.GetValueOrNull(roomId) is { } identifier
                ? DecorationIdentifier.FromString(identifier)
                : null;

        public void SetWallpaper(DecorationIdentifier identifier, string roomId)
        {
            decoratableLocation.SetWallpaper(identifier.ToString(), roomId);
        }

        public DecorationIdentifier? GetFloor(string roomId) =>
            decoratableLocation.appliedFloor.GetValueOrNull(roomId) is { } identifier
                ? DecorationIdentifier.FromString(identifier)
                : null;

        public void SetFloor(DecorationIdentifier identifier, string roomId)
        {
            decoratableLocation.SetFloor(identifier.ToString(), roomId);
        }
    }
}

//////////////////////////////////

public static class DecorationIdHelper
{
    public static string ToString(TextureIdentifierWithoutSeason identifier) =>
        $"{identifier.Owner}.{identifier.ForModel.Type}.{identifier.ForModel.String}.{identifier.Variation}";

    // public static string ToString(TextureIdentifierWithoutSeason identifier) =>
    //     $"{identifier.Owner}.{identifier.ForModel.Type}.{identifier.ForModel.String}.{identifier.Variation}:0";

    public static TextureIdentifierWithoutSeason FromString(string identifier, ModelIdentifier forModel)
    {
        // return int.TryParse(identifier, out var vanillaNumber)
        //     ? new(AlternativeTextures.DEFAULT_OWNER, forModel, vanillaNumber)
        //     : identifier.Split(":") switch
        //     {
        //         [var actual, var _] => actual.Split(".") switch
        //         {
        //             [.. var owner, var typeString, var name, var variationString]
        //                 when Enum.TryParse<TextureType>(typeString, out var type)
        //                     && int.TryParse(variationString, out var variation) => new TextureIdentifierWithoutSeason(
        //                 string.Join(".", owner),
        //                 type.WithName(name),
        //                 variation
        //             ),
        //             _ => throw new ArgumentException($"Couln't parse identifier '{identifier}'"),
        //         },
        //         _ => throw new ArgumentException($"Couln't parse identifier '{identifier}'"),
        //     };

        return int.TryParse(identifier, out var vanillaNumber)
            ? new(AlternativeTextures.DEFAULT_OWNER, forModel, vanillaNumber)
            : identifier.Split(".") switch
            {
                [.. var owner, var typeString, var name, var variationString]
                    when Enum.TryParse<TextureType>(typeString, out var type)
                        && int.TryParse(variationString, out var variation) => new TextureIdentifierWithoutSeason(
                    string.Join(".", owner),
                    type.WithName(name),
                    variation
                ),
                _ => throw new ArgumentException($"Couln't parse identifier '{identifier}'"),
            };
    }
}

record WallpaperDecorationPaintable(DecoratableLocation location, string roomId) : IPaintable
{
    public ModelIdentifier ModelIdentifier { get; } = ModelIdentifier.Wallpaper;

    public object? Related { get; } = null;

    public TextureIdentifierWithoutSeason? TextureIdentifier =>
        location.GetWallpaper(roomId) switch
        {
            { Name: null or "", Variant: var variant } => new(
                AlternativeTextures.DEFAULT_OWNER,
                ModelIdentifier.Wallpaper,
                variant
            ),
            { Name: { } name } => DecorationIdHelper.FromString(name, ModelIdentifier.Wallpaper),
            null => null,
        };

    public void ApplyTexture(TextureIdentifierWithoutSeason? maybeTextureToApply)
    {
        if (maybeTextureToApply is { } textureToApply)
        {
            var decorationKey = textureToApply.IsDefault
                ? new DecorationIdentifier(null, textureToApply.Variation == -1 ? 0 : textureToApply.Variation)
                : new DecorationIdentifier(DecorationIdHelper.ToString(textureToApply), 0);
            location.SetWallpaper(decorationKey, roomId);
        }
        else
        {
            location.SetWallpaper("0", roomId);
        }
    }

    public DrawableTexture? PreviewTexture(UniqueTextureIdentifier textureIdentifier)
    {
        if (textureIdentifier.IsDefault)
        {
            var which = textureIdentifier.Variation;
            return new()
            {
                Texture = Game1.content.Load<Texture2D>("Maps\\walls_and_floors"),
                SourceRect = new Rectangle(which % 16 * 16, which / 16 * 48, 16, 48),
            };
        }
        else
        {
            var textureModel = AlternativeTextures.textureManager.GetTexture(textureIdentifier);
            return textureModel?.Texture.WithSourceRect(
                new()
                {
                    X = 0,
                    Y = 0,
                    Width = 16,
                    Height = 48,
                }
            );
        }
    }
}

record FloorDecorationPaintable(DecoratableLocation location, string roomId) : IPaintable
{
    public ModelIdentifier ModelIdentifier { get; } = ModelIdentifier.Floor;

    public object? Related { get; } = null;

    public TextureIdentifierWithoutSeason? TextureIdentifier =>
        location.GetFloor(roomId) switch
        {
            { Name: null, Variant: var variant } => new(
                AlternativeTextures.DEFAULT_OWNER,
                ModelIdentifier.Wallpaper,
                variant
            ),
            { Name: { } name } => DecorationIdHelper.FromString(name, ModelIdentifier.Wallpaper),
            null => null,
        };

    public void ApplyTexture(TextureIdentifierWithoutSeason? maybeTextureToApply)
    {
        if (maybeTextureToApply is { } textureToApply)
        {
            var decorationKey = textureToApply.IsDefault
                ? new DecorationIdentifier(null, textureToApply.Variation == -1 ? 0 : textureToApply.Variation)
                : new DecorationIdentifier(DecorationIdHelper.ToString(textureToApply), 0);
            location.SetFloor(decorationKey, roomId);
        }
        else
        {
            location.SetFloor("0", roomId);
        }
    }

    public DrawableTexture? PreviewTexture(UniqueTextureIdentifier textureIdentifier)
    {
        if (textureIdentifier.IsDefault)
        {
            var which = textureIdentifier.Variation;
            return new()
            {
                Texture = Game1.content.Load<Texture2D>("Maps\\walls_and_floors"),
                SourceRect = new Rectangle(which % 8 * 32, 336 + (which / 8 * 32), 32, 32),
            };
        }
        else
        {
            var textureModel = AlternativeTextures.textureManager.GetTexture(textureIdentifier);
            return textureModel?.Texture.WithSourceRect(
                new()
                {
                    X = 0,
                    Y = 0,
                    Width = 32,
                    Height = 32,
                }
            );
        }
    }
}
