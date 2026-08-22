using StardewValley.Locations;

namespace AlternativeTextures.Framework.Paintables;

record WallpaperDecorationPaintable(DecoratableLocation location, string roomId) : IPaintable
{
    public ModelIdentifier ModelIdentifier { get; } = ModelIdentifier.Wallpaper;

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
}

record FloorDecorationPaintable(DecoratableLocation location, string roomId) : IPaintable
{
    public ModelIdentifier ModelIdentifier { get; } = ModelIdentifier.Floor;

    public TextureIdentifierWithoutSeason? TextureIdentifier =>
        location.GetFloor(roomId) switch
        {
            { Name: null or "", Variant: var variant } => new(
                AlternativeTextures.DEFAULT_OWNER,
                ModelIdentifier.Floor,
                variant
            ),
            { Name: { } name } => DecorationIdHelper.FromString(name, ModelIdentifier.Floor),
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
}
