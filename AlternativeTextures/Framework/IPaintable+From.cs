using AlternativeTextures.Framework.Paintables;
using StardewValley.Buildings;
using StardewValley.TerrainFeatures;

namespace AlternativeTextures.Framework;

public static class IPaintable_From
{
    extension(IPaintable paintable)
    {
        public static IPaintable From(StardewValley.Object obj) =>
            new PaintableFromModData(obj.modData) { ModelIdentifier = ModelIdentifier.From(obj) };

        public static IPaintable? From(TerrainFeature terrainFeature) =>
            ModelIdentifier.From(terrainFeature) is { } modelIdentifier
                ? terrainFeature switch
                {
                    Tree tree => new PaintableTree(tree) { ModelIdentifier = modelIdentifier },
                    _ => new PaintableFromModData(terrainFeature.modData) { ModelIdentifier = modelIdentifier },
                }
                : null;

        public static IPaintable From(Building building) =>
            new PaintableBuilding(building) { ModelIdentifier = ModelIdentifier.From(building) };

        public static IPaintable? From(WorldObject worldObject) =>
            worldObject switch
            {
                WorldObject.TerrainFeature(var terrainFeature) => From(terrainFeature),
                WorldObject.Object(var @object) => From(@object),
                WorldObject.Decoration decoration => decoration switch
                {
                    { Type: DecorationType.Floor } => new FloorDecorationPaintable(
                        decoration.Location,
                        decoration.RoomId
                    ),
                    { Type: DecorationType.Wallpaper } => new WallpaperDecorationPaintable(
                        decoration.Location,
                        decoration.RoomId
                    ),
                },
                WorldObject.Mailbox(var farm) => new MailboxPaintable(farm),
                WorldObject.Building(var building) => From(building),
            };

        public static IPaintable? From(WorldObjectUnion worldObject) =>
            worldObject switch
            {
                TerrainFeature terrainFeature => From(terrainFeature),
                StardewValley.Object @object => From(@object),
                Decoration decoration => decoration switch
                {
                    { Type: DecorationType.Floor } => new FloorDecorationPaintable(
                        decoration.Location,
                        decoration.RoomId
                    ),
                    { Type: DecorationType.Wallpaper } => new WallpaperDecorationPaintable(
                        decoration.Location,
                        decoration.RoomId
                    ),
                },
                Mailbox mailbox => new MailboxPaintable(mailbox.Farm),
                Building building => From(building),
            };
    }
}
