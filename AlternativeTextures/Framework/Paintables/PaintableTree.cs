using StardewValley.TerrainFeatures;

namespace AlternativeTextures.Framework.Paintables;

record PaintableTree(Tree Tree) : IPaintable
{
    public required ModelIdentifier ModelIdentifier { get; init; }

    private PaintableFromModData modDataPaintable => new(Tree.modData) { ModelIdentifier = ModelIdentifier };

    public TextureIdentifierWithoutSeason? TextureIdentifier =>
        modDataPaintable.TextureIdentifier is { } textureIdentifier ? textureIdentifier : null;

    public void ApplyTexture(TextureIdentifierWithoutSeason? maybeTexture)
    {
        modDataPaintable.ApplyTexture(maybeTexture);
        Tree.resetTexture();
        /// TODO Broadcast resetTexture to other players
    }
}
