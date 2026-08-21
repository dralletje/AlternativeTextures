namespace AlternativeTextures.Framework;

public interface IPaintable
{
    ModelIdentifier ModelIdentifier { get; }
    TextureIdentifierWithoutSeason? TextureIdentifier { get; }

    public void ApplyTexture(TextureIdentifierWithoutSeason? texture);
}
