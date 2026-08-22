using StardewValley.Buildings;

namespace AlternativeTextures.Framework.Paintables;

record PaintableBuilding(Building Building) : IPaintable
{
    public required ModelIdentifier ModelIdentifier { get; init; }

    private PaintableFromModData modDataPaintable => new(Building.modData) { ModelIdentifier = ModelIdentifier };

    public TextureIdentifierWithoutSeason? TextureIdentifier =>
        modDataPaintable.TextureIdentifier is { } textureIdentifier
            ? textureIdentifier
            : new(Building.skinId.Value, ModelIdentifier, -1);

    public void ApplyTexture(TextureIdentifierWithoutSeason? maybeTexture)
    {
        if (maybeTexture is not { } texture)
        {
            modDataPaintable.ApplyTexture(null);
        }
        else if (texture.IsDefault)
        {
            modDataPaintable.ApplyTexture(null);
            Building.skinId.Value = texture.Owner;
        }
        else
        {
            modDataPaintable.ApplyTexture(texture);
        }

        Building.resetTexture();
        /// TODO Broadcast resetTexture to other players
    }
}
