using Incubator;
using StardewValley.Mods;

namespace AlternativeTextures.Framework.Paintable;

/// TODO Validate TextureIdent against the ModelIdentifier ??
record PaintableFromModData(ModDataDictionary modData) : IPaintable
{
    public required ModelIdentifier ModelIdentifier { get; init; }

    public TextureIdentifierWithoutSeason? TextureIdentifier =>
        (
            modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) is { } name
            && modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION) is { } variation
                ? TextureIdentifierWithoutSeason.FromString(name, variation)
                : null
        );

    public void ApplyTexture(TextureIdentifierWithoutSeason? maybeTexture)
    {
        if (maybeTexture is { } texture)
        {
            modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = texture.Owner;
            modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = texture.LegacyId;
            modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = texture.Variation.ToString();
        }
        else
        {
            modData.Remove(ModDataKeys.ALTERNATIVE_TEXTURE_OWNER);
            modData.Remove(ModDataKeys.ALTERNATIVE_TEXTURE_NAME);
            modData.Remove(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION);
        }
    }
}
