using System;
using System.Text.Json.Serialization;
using Incubator;

namespace AlternativeTextures.Framework;

public enum TextureType
{
    Unknown,
    Craftable,
    Grass,
    Tree,
    FruitTree,
    Crop,
    GiantCrop,
    ResourceClump,
    Bush,
    Flooring,
    Furniture,
    Character,
    Building,
    Decoration,
    ArtifactSpot,
}

public static class TextureTypeExtensions
{
    public static ModelIdentifier WithName(this TextureType type, string name) =>
        new()
        {
            Type = type,
            IsName = true,
            String = name,
        };
}

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
}

public static class ModelIdentifierExtensions
{
    extension(ModelIdentifier modelIdentifier)
    {
        public string Name => modelIdentifier.String;

        /// TODO Make this work for IDs too?
        public static ModelIdentifier? FromString(string modelIdentifierString)
        {
            return modelIdentifierString.Split("_", 2) switch
            {
                [var typeString, var name] => Enum.ParseOrNull<TextureType>(typeString) switch
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

        public static ModelIdentifier Floor => TextureType.Decoration.WithName("Floor");
        public static ModelIdentifier Wallpaper => TextureType.Decoration.WithName("Wallpaper");
        public static ModelIdentifier Mailbox => TextureType.Building.WithName("Mailbox");
    }
}
