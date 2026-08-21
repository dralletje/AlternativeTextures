using AlternativeTextures.Framework.Paintable;
using Incubator;
using StardewValley.Buildings;
using StardewValley.Mods;
using StardewValley.TerrainFeatures;

namespace AlternativeTextures.Framework;

public static class IPaintable_From
{
    extension(IPaintable paintable)
    {
        public static IPaintable? From(StardewValley.Object obj) =>
            new PaintableFromModData(obj.modData) { ModelIdentifier = ModelIdentifier.From(obj) };

        public static IPaintable? From(TerrainFeature terrainFeature) =>
            ModelIdentifier.From(terrainFeature) is { } modelIdentifier
                ? new PaintableFromModData(terrainFeature.modData) { ModelIdentifier = modelIdentifier }
                : null;

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
                    { Type: DecorationType.Wallpaper } => new FloorDecorationPaintable(
                        decoration.Location,
                        decoration.RoomId
                    ),
                },
                WorldObject.Mailbox(var farm) => new MailboxPaintable(farm),
                WorldObject.Building(var building) => new PaintableBuilding(building)
                {
                    ModelIdentifier = ModelIdentifier.From(building),
                },
            };
    }
}
