using Incubator;
using StardewValley.Locations;

namespace AlternativeTextures.Stardew;

public record DecorationIdentifier(string? Name, int Variant)
{
    public bool IsVanilla => Name is null;

    public override string ToString() => Name is { } name ? $"{name}:{Variant}" : $"{Variant}";

    public static DecorationIdentifier? FromString(string identifier) =>
        identifier.Split(":") switch
        {
            [var name, .. var nameRest, var versionString] when int.TryParse(versionString, out var version) => new(
                $"{name}{string.Join(":", nameRest)}",
                version
            ),
            [var defaultVariantString] when int.TryParse(defaultVariantString, out var defaultVariant) => new(
                null,
                defaultVariant
            ),
            _ => null,
        };
}

public static class StardewDecorationExtensions
{
    extension(DecoratableLocation decoratableLocation)
    {
        public DecorationIdentifier? GetWallpaper(string roomId) =>
            decoratableLocation.appliedWallpaper.GetValueOrNull(roomId) is { } identifier
                ? DecorationIdentifier.FromString(identifier)
                : null;

        public void SetWallpaper(DecorationIdentifier identifier, string roomId)
        {
            decoratableLocation.SetWallpaper(identifier.ToString(), roomId);
        }

        public DecorationIdentifier? GetFloor(string roomId) =>
            decoratableLocation.appliedFloor.GetValueOrNull(roomId) is { } identifier
                ? DecorationIdentifier.FromString(identifier)
                : null;

        public void SetFloor(DecorationIdentifier identifier, string roomId)
        {
            decoratableLocation.SetFloor(identifier.ToString(), roomId);
        }
    }
}
