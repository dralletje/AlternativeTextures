using AlternativeTextures.Framework;
using Incubator;
using StardewValley.Buildings;

record PaintableBuilding(Building Building) : IPaintable
{
    public required ModelIdentifier ModelIdentifier { get; init; }

    public TextureIdentifierWithoutSeason? TextureIdentifier =>
        (
            Building.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) is { } name
            && Building.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION) is { } variation
                ? TextureIdentifierWithoutSeason.FromString(name, variation)
                : null
        );

    public void ApplyTexture(TextureIdentifierWithoutSeason? maybeTexture)
    {
        if (maybeTexture is { } texture)
        {
            Building.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = texture.Owner;
            Building.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = texture.LegacyId;
            Building.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = texture.Variation.ToString();
            Building.resetTexture();
        }
        else
        {
            Building.modData.Remove(ModDataKeys.ALTERNATIVE_TEXTURE_OWNER);
            Building.modData.Remove(ModDataKeys.ALTERNATIVE_TEXTURE_NAME);
            Building.modData.Remove(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION);
            Building.resetTexture();
        }
    }
}
