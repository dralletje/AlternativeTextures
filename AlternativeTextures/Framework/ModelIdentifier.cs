using System.Text.Json.Serialization;

namespace AlternativeTextures.Framework;

public record ModelIdentifier
{
    [JsonInclude]
    public required TextureType Type;

    [JsonInclude]
    public required bool IsName;

    [JsonInclude]
    public required string String;

    public override string ToString()
    {
        return $"{String} ({Type})";
    }

    public string Name => String;

    /// TODO Make this work for IDs too?
    public static ModelIdentifier? FromString(string modelIdentifierString)
    {
        return modelIdentifierString.Split("_", 2) switch
        {
            [var typeString, var name] => EnumUtil.ParseOrNull<TextureType>(typeString) switch
            {
                { } modelType => new ModelIdentifier()
                {
                    Type = modelType,
                    IsName = true,
                    String = name,
                },
                null => null,
            },
            _ => null,
        };
    }

    public static readonly ModelIdentifier Floor = TextureType.Decoration.WithName("Floor");
    public static readonly ModelIdentifier Wallpaper = TextureType.Decoration.WithName("Wallpaper");
}
