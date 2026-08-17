using System;
using System.Collections.Generic;
using System.Linq;
using AlternativeTextures.Framework;
using AlternativeTextures.Framework.Models;
using AlternativeTextures.Framework.Utilities;
using ConsoleLog;
using Force.DeepCloner;
using HarmonyLib;
using Incubator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Mods;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace AlternativeTextures.Tools;

class PaintBrushTool(IModHelper helper, GenericTool tool) : ICustomTool
{
    internal const string PAINT_BRUSH_FLAG = "AlternativeTextures.PaintBrushFlag";
    internal const string PAINT_BRUSH_SCALE = "AlternativeTextures.PaintBrushScale";

    string? Type
    {
        get
        {
            var type = tool.modData.GetValueOrDefault(PAINT_BRUSH_FLAG);
            return string.IsNullOrEmpty(type) ? null : type;
        }
    }
    TextureIdentifier? Texture
    {
        get
        {
            return
                tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] is { } owner
                && tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] is { } name
                && tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] is { } variation
                ? new TextureIdentifier(owner, name, variation)
                : null;
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

    private bool is_dragging = false;

    static readonly AccessTools.FieldRef<TerrainFeature, ModDataDictionary> modDataRef =
        AccessTools.FieldRefAccess<TerrainFeature, ModDataDictionary>("<modData>k__BackingField");

    public void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (this.Type is { } objectType)
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
            else
            {
                var paintables = IPaintable.OnTile(targetTile);
                var target = paintables.FirstOrDefault(paintable => paintable.Type == objectType);

                var positionOnScreen = Game1.GlobalToLocal(
                    Game1.viewport,
                    targetTile.ToVector2() * Game1.tileSize
                );
                var overlayColor = target is not null ? Color.Green * 0.7f : Color.Red * 0.7f;

                e.SpriteBatch.Draw(
                    Game1.mouseCursors,
                    new Rectangle(
                        (int)positionOnScreen.X,
                        (int)positionOnScreen.Y,
                        Game1.tileSize,
                        Game1.tileSize
                    ),
                    new Rectangle(194, 388, 16, 16), // Source rect for vanilla placement square
                    overlayColor,
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0.0001f // Draw depth (just above ground)
                );

                if (this.Texture is { } texture && target?.Related is TerrainFeature floor)
                {
                    var newfloor = floor.ShallowClone();
                    var clonedModData = new ModDataDictionary();

                    clonedModData.CopyFrom(floor.modData); // Or populate as needed
                    var paintable = new PaintableFromModData(clonedModData)
                    {
                        ModelIdentifier = new()
                        {
                            Type = AlternativeTextureModel.TextureType.Unknown,
                            Name = "",
                        },
                        Related = null,
                    };
                    paintable.ApplyTexture(texture);
                    modDataRef(newfloor) = clonedModData;

                    newfloor.draw(e.SpriteBatch);
                }
                // else if (target?.Related is StardewValley.Object obj)
                // {
                //     Console.Log($"Related: {obj}");
                //     var pixels = targetTile * Game1.tileSize;
                //     obj.draw(e.SpriteBatch, pixels.X, pixels.Y, 0.5f);
                // }
                else if (this.Texture is { } justtexture && Type is { } type)
                {
                    var textureModel = AlternativeTextures.textureManager.GetSpecificTextureModel(
                        justtexture.Name
                    );
                    if (textureModel is null)
                    {
                        Console.Log($"textureModel is null ({justtexture.Name})");
                        return;
                    }

                    var textureVariation = int.Parse(justtexture.Variation);
                    if (
                        textureVariation == -1
                        || AlternativeTextures.modConfig.IsTextureVariationDisabled(
                            textureModel.GetId(),
                            textureVariation
                        )
                    )
                    {
                        Console.Log($"textureVariation is -1");
                        return;
                    }
                    var textureOffset = textureModel.GetTextureOffset(textureVariation);
                    var texture2d = textureModel.GetTexture(textureVariation);

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
                    //                 Type = AlternativeTextureModel.TextureType.Unknown,
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

                    if (type.StartsWith("Flooring_"))
                    {
                        e.SpriteBatch.Draw(
                            texture2d,
                            new Rectangle(
                                (int)positionOnScreen.X,
                                (int)positionOnScreen.Y,
                                Game1.tileSize,
                                Game1.tileSize
                            ),
                            new Rectangle(
                                0,
                                0,
                                Game1.tileSize / Game1.pixelZoom,
                                Game1.tileSize / Game1.pixelZoom
                            ),
                            target is not null ? Color.White * 0.7f : Color.White * 0.3f
                        );
                    }
                    else
                    {
                        e.SpriteBatch.Draw(
                            texture2d,
                            new Rectangle(
                                (int)positionOnScreen.X
                                    + (
                                        Game1.tileSize - textureModel.TextureWidth * Game1.pixelZoom
                                    ),
                                (int)positionOnScreen.Y
                                    + (
                                        Game1.tileSize
                                        - textureModel.TextureHeight * Game1.pixelZoom
                                    ),
                                textureModel.TextureWidth * Game1.pixelZoom,
                                textureModel.TextureHeight * Game1.pixelZoom
                            ),
                            new Rectangle(
                                0,
                                0,
                                textureModel.TextureWidth,
                                textureModel.TextureHeight
                            ),
                            // new Rectangle(sourceRectPosition * 16 % 256, (sourceRectPosition / 16 * 16) + textureOffset, 16, 16),

                            target is not null
                                ? Color.White * 0.7f
                                : Color.White * 0.3f
                        );
                    }
                }
                else
                {
                    Console.Log($"this.Texture: {this.Texture}");
                }
                // if (this.Texture is { } texture && target?.Related is Flooring floor) {
                //     var neighborMaskAccessor = AccessTools.FieldRefAccess<Flooring, byte>("neighborMask");
                //     var myfloor = new Flooring(floor.whichFloor.ToString())
                //     {
                //         Tile = targetTile.ToVector2(),
                //         // floorTexture = texture2d,
                //         Location = Game1.currentLocation,

                //     };
                //     neighborMaskAccessor(myfloor) = neighborMaskAccessor(floor);
                //     var paintable = new PaintableFromModData("", myfloor.modData, null)
                //     {
                //         Texture = texture
                //     };
                //     myfloor.draw(e.SpriteBatch);
                // }
            }
        }
        else
        {
            return;
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
            var tile = Game1.player.ActiveTargetTile;
            PrettyPrint.Log("[IsUseToolButton] Paint Brush");

            var placedObject = Game1.currentLocation.getObjectAtTile(tile.X, tile.Y);
            if (placedObject?.QualifiedItemId == AlternativeTextures.PAINTPAIL)
            {
                PrettyPrint.Log("Cleaning using the PAINTPAIL");
                tool.modData[PAINT_BRUSH_FLAG] = string.Empty;
                tool.modData[PAINT_BRUSH_SCALE] = 0.5f.ToString();
                yield return false;
            }
            else
            {
                if (Type is null)
                {
                    PrettyPrint.Log("[IsUseToolButton] Getting texture");
                    DoReadTexture(tile);
                    yield return false;
                }
                else
                {
                    this.is_dragging = true;
                    try
                    {
                        while (true)
                        {
                            DoApplyTexture(Game1.player.ActiveTargetTile);
                            yield return true;
                        }
                    }
                    finally
                    {
                        this.is_dragging = false;
                    }
                }
            }
        }
    }

    ////////////////////////////////////////////

    private void DoReadTexture(Tile tile)
    {
        var paintables = IPaintable.OnTile(tile);
        Console.Log($"x {10:raw:red}");
        Console.Log($"paintables: {paintables.Select(x => x.Type)}");

        var paintableMaybe = IPaintable.OnTile(tile).FirstOrDefault();
        if (paintableMaybe is { } paintable)
        {
            Console.Log($"paintable: {paintable.Category} - {paintable.InstanceName}");
            Console.Log(
                $"paintable.Texture: {paintable.Texture?.Owner} - {paintable.Texture?.Name}"
            );
            tool.modData[PAINT_BRUSH_FLAG] = paintable.Type;
            tool.modData[PAINT_BRUSH_SCALE] = 0.5f.ToString();
            tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = paintable.Texture?.Owner;
            tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = paintable.Texture?.Name;
            tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = paintable.Texture?.Variation;
        }
    }

    private void DoApplyTexture(Tile tile)
    {
        // TextureIdentifier? toolTexture = tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] is { } owner
        //         && tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] is { } name
        //         && tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] is { } variation
        //         ? new TextureIdentifier(owner, name, variation) : null;
        var paintables = IPaintable.OnTile(tile);

        foreach (var paintable in paintables)
        {
            if (paintable.Type == this.Type)
            {
                paintable.ApplyTexture(this.Texture);
                return;
            }
        }

        /// Don't do anything if none match
    }

    ////////////////////////////////////////////

    public static PaintBrushTool? From(IModHelper helper, Tool? tool)
    {
        if (tool is GenericTool genericTool)
        {
            if (tool.modData.ContainsKey(PAINT_BRUSH_FLAG))
            {
                return new PaintBrushTool(helper, genericTool);
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }
    }
}
