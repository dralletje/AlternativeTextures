using Incubator;
using StardewValley;

namespace AlternativeTextures.Framework.Paintable;

/// TODO Validate TextureIdent against the ModelIdentifier ??
public record MailboxPaintable(GameLocation Location) : IPaintable
{
    const string TEXTURE_NAME_KEY = "AlternativeTextureName.Mailbox";
    const string TEXTURE_VARIATION_KEY = "AlternativeTextureVariation.Mailbox";
    const string TEXTURE_OWNER_KEY = "AlternativeTextureOwner.Mailbox";

    public ModelIdentifier ModelIdentifier { get; } = ModelIdentifier.Mailbox;

    public TextureIdentifierWithoutSeason? TextureIdentifier =>
        (
            Location.modData.GetValueOrNull(TEXTURE_NAME_KEY) is { } name
            && Location.modData.GetValueOrNull(TEXTURE_VARIATION_KEY) is { } variation
                ? TextureIdentifierWithoutSeason.FromString(name, variation)
                : null
        );

    public void ApplyTexture(TextureIdentifierWithoutSeason? maybeTexture)
    {
        if (maybeTexture is { } texture)
        {
            Location.modData[TEXTURE_OWNER_KEY] = texture.Owner;
            Location.modData[TEXTURE_NAME_KEY] = texture.LegacyId;
            Location.modData[TEXTURE_VARIATION_KEY] = texture.Variation.ToString();
        }
        else
        {
            Location.modData.Remove(TEXTURE_OWNER_KEY);
            Location.modData.Remove(TEXTURE_NAME_KEY);
            Location.modData.Remove(TEXTURE_VARIATION_KEY);
        }
    }
}
