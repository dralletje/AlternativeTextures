using System;
using System.Collections.Generic;
using System.Linq;
using AlternativeTextures.CustomToolMod;
using AlternativeTextures.Framework;
using AlternativeTextures.Framework.Paintable;
using Force.DeepCloner;
using HarmonyLib;
using Incubator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
// using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Mods;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace AlternativeTextures.App.Tools;

class PaintBrushFilledTool(IModHelper helper, GenericTool tool) : ICustomTool
{
    internal const string MODDATA_MODEL_KEY = "AlternativeTextures.PaintBrush.Model";
    internal const string MODDATA_TEXTURE_KEY = "AlternativeTextures.PaintBrush.Texture";

    ModelIdentifier ModelIdentifier
    {
        get
        {
            var modelIdentifierString = tool.modData.GetValueOrNull(MODDATA_MODEL_KEY);
            /// TODO Should not hit "Craftable_Chest", but would still like a more thoughtout fallback
            return ModelIdentifier.FromString(modelIdentifierString ?? "") ?? TextureType.Craftable.WithName("Chest");
        }
    }

    TextureIdentifierWithoutSeason Texture
    {
        get
        {
            try
            {
                var json = tool.modData[MODDATA_TEXTURE_KEY];
                return JsonConvert.DeserializeObject<TextureIdentifierWithoutSeason>(json)
                    ?? TextureIdentifierWithoutSeason.DefaultFor(ModelIdentifier);
            }
            catch
            {
                return TextureIdentifierWithoutSeason.DefaultFor(ModelIdentifier);
            }
        }
    }

    //////////////////////////////////

    public IDisposable? Start()
    {
        helper.Events.Display.RenderedWorld += OnRenderedWorld;
        return new ActionDisposable(() =>
        {
            helper.Events.Display.RenderedWorld -= OnRenderedWorld;
        });
    }

    static readonly AccessTools.FieldRef<TerrainFeature, ModDataDictionary> modDataRef = AccessTools.FieldRefAccess<
        TerrainFeature,
        ModDataDictionary
    >("<modData>k__BackingField");

    record PaintableOnTheWorld(WorldObject WorldObject, IPaintable Paintable);

    public void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        var targetTile = Game1.player.ActiveTargetTile;
        if (
            Game1.currentLocation.getObjectAtTile(targetTile.X, targetTile.Y)?.QualifiedItemId
            == AlternativeTextures.PAINTPAIL
        )
        {
            /// Don't show any outline when hovering over the Paint Pail
            return;
        }

        var worldObjects = IPaintable.GetAnythingAtTile(targetTile);
        var target = worldObjects
            .Select(worldObject =>
                IPaintable.From(worldObject) is { } paintable ? new PaintableOnTheWorld(worldObject, paintable) : null
            )
            .WhereNotNull()
            .FirstOrDefault(paintable => paintable.Paintable.ModelIdentifier == this.ModelIdentifier);

        var positionOnScreen = Game1.GlobalToLocal(Game1.viewport, targetTile.ToVector2() * Game1.tileSize);
        var overlayColor = target is not null ? Color.Green * 1f : Color.Red * 0.7f;

        e.SpriteBatch.Draw(
            Game1.mouseCursors,
            new Rectangle((int)positionOnScreen.X, (int)positionOnScreen.Y, Game1.tileSize, Game1.tileSize),
            new Rectangle(194, 388, 16, 16), // Source rect for vanilla placement square
            overlayColor,
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            0.0001f // Draw depth (just above ground)
        );

        if (this.Texture is { } texture && target?.WorldObject is WorldObject.TerrainFeature(var floor))
        {
            var newfloor = floor.ShallowClone();
            var clonedModData = new ModDataDictionary();

            clonedModData.CopyFrom(floor.modData); // Or populate as needed
            var paintable = new PaintableFromModData(clonedModData)
            {
                ModelIdentifier = TextureType.Unknown.WithName(""),
            };
            paintable.ApplyTexture(texture);
            modDataRef(newfloor) = clonedModData;

            newfloor.draw(e.SpriteBatch);
        }
        else
        {
            var textureModel = AlternativeTextures.textureManager.GetTexture(
                Texture.WithSeason(Game1.currentLocation.GetSeason())
            );
            if (textureModel is null)
            {
                return;
            }

            // if (type.StartsWith("Craftable_"))
            // {
            //     var name = type["Craftable_".Length..];
            //     Console.Log($">> Name: {name}");

            //     var maybeItem = ItemRegistry.ItemTypes.SelectMany(type => type.GetAllData()).FirstOrDefault(data => data.InternalName == name);

            //     if (maybeItem is { } item)
            //     {
            //         var obj = ItemRegistry.Create<StardewValley.Object>(item.QualifiedItemId);
            //         var paintable = new PaintableFromModData(obj.modData)
            //         {
            //             ModelIdentifier = new()
            //             {
            //                 Type = TextureType.Unknown,
            //                 Name = "",
            //             },
            //             Related = null,

            //             /// This is actually an effect!
            //             /// This will update the texture on the modData provided
            //             Texture = justtexture,
            //         };

            //         obj.drawInMenu(e.SpriteBatch, positionOnScreen, 0.7f);
            //         return;
            //     }
            //     // var data = ItemRegistry.GetData($"{name}");
            //     Console.Log($">> data: {maybeItem?.QualifiedItemId}");
            // }

            // var data = ItemRegistry.GetData(justtexture.Name);

            if (ModelIdentifier.Type == TextureType.Flooring)
            {
                e.SpriteBatch.Draw(
                    textureModel.Texture.WithSourceRect(
                        new Rectangle(0, 0, Game1.tileSize / Game1.pixelZoom, Game1.tileSize / Game1.pixelZoom)
                    ),
                    new Rectangle((int)positionOnScreen.X, (int)positionOnScreen.Y, Game1.tileSize, Game1.tileSize),
                    target is not null ? Color.White * 0.7f : Color.White * 0.3f
                );
            }
            else
            {
                e.SpriteBatch.Draw(
                    textureModel.Texture.WithSourceRect(
                        new Rectangle(0, 0, textureModel.TextureWidth, textureModel.TextureHeight)
                    ),
                    new Rectangle(
                        (int)positionOnScreen.X + (Game1.tileSize - (textureModel.TextureWidth * Game1.pixelZoom)),
                        (int)positionOnScreen.Y + (Game1.tileSize - (textureModel.TextureHeight * Game1.pixelZoom)),
                        textureModel.TextureWidth * Game1.pixelZoom,
                        textureModel.TextureHeight * Game1.pixelZoom
                    ),
                    // new Rectangle(sourceRectPosition * 16 % 256, (sourceRectPosition / 16 * 16) + textureOffset, 16, 16),

                    target is not null
                        ? Color.White * 0.7f
                        : Color.White * 0.3f
                );
            }
        }
    }

    public IEnumerator<bool> OnButton(ButtonPressedEventArgs e)
    {
        if (e.Button is SButton.MouseRight)
        {
            var tile = new Tile(e.Cursor.Tile);
            PrettyPrint.Log("[MouseRight] Paint Brush");
            DoReadTexture(tile);
            yield return true;
        }
        else if (e.Button.IsUseToolButton())
        {
            var textureModel = AlternativeTextures.textureManager.GetTexture(
                Texture.WithSeason(Game1.currentLocation.GetSeason())
            );

            var tile = Game1.player.ActiveTargetTile;
            Console.Log($"[IsUseToolButton] Paint Brush");

            var placedObject = Game1.currentLocation.getObjectAtTile(tile.X, tile.Y);
            if (placedObject?.QualifiedItemId == AlternativeTextures.PAINTPAIL)
            {
                Console.Log($"Cleaning using the PAINTPAIL");
                Game1.player.Items[Game1.player.CurrentToolIndex] = PaintBrushEmptyTool.CreateItem();
                yield return false;
            }
            else
            {
                while (true)
                {
                    DoApplyTexture(Game1.player.ActiveTargetTile);
                    yield return true;
                }
            }
        }
    }

    ////////////////////////////////////////////

    private void DoReadTexture(Tile tile)
    {
        var paintableMaybe = IPaintable
            .GetAnythingAtTile(tile)
            .Select(x => IPaintable.From(x))
            .WhereNotNull()
            .FirstOrDefault();
        if (paintableMaybe is { } paintable)
        {
            var item = PaintBrushFilledTool.CreateItem(
                paintable.TextureIdentifier ?? TextureIdentifierWithoutSeason.DefaultFor(paintable.ModelIdentifier)
            );
            Game1.player.Items[Game1.player.CurrentToolIndex] = item;
        }
    }

    private void DoApplyTexture(Tile tile)
    {
        var paintables = IPaintable.GetAnythingAtTile(tile).Select(x => IPaintable.From(x)).WhereNotNull();
        foreach (var paintable in paintables)
        {
            if (paintable.ModelIdentifier == this.ModelIdentifier)
            {
                paintable.ApplyTexture(this.Texture);
                return;
            }
        }

        /// Don't do anything if none match
    }

    public static Item CreateItem(TextureIdentifierWithoutSeason texture)
    {
        var tool = ItemRegistry.Create(AlternativeTextures.PAINT_BRUSH_FILLED_ID);
        tool.modData[MODDATA_MODEL_KEY] = $"{texture.ForModel.Type}_{texture.ForModel.String}";
        tool.modData[MODDATA_TEXTURE_KEY] = JsonConvert.SerializeObject(texture);
        return tool;
    }

    public static PaintBrushFilledTool? From(IModHelper helper, Tool? tool)
    {
        return tool is GenericTool { QualifiedItemId: AlternativeTextures.PAINT_BRUSH_FILLED_ID } genericTool
            ? new PaintBrushFilledTool(helper, genericTool)
            : null;
    }
}
