using System;

namespace AlternativeTextures.Framework;

public static class DecorationIdHelper
{
    public static string ToString(TextureIdentifierWithoutSeason identifier) =>
        $"{identifier.Owner}.{identifier.ForModel.Type}.{identifier.ForModel.String}.{identifier.Variation}";

    public static TextureIdentifierWithoutSeason FromString(string identifier, ModelIdentifier forModel) =>
        int.TryParse(identifier, out var vanillaNumber)
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
