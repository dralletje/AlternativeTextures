using System;
using System.Collections.Generic;
using System.Linq;
using AlternativeTextures.Framework.Models;
using AlternativeTextures.Framework.Patches;
using AlternativeTextures.Framework.Patches.Buildings;
using AlternativeTextures.Framework.Utilities;
using ConsoleLog;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using Newtonsoft.Json;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.GameData.FloorsAndPaths;
using StardewValley.GameData.GiantCrops;
using StardewValley.Internal;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using static AlternativeTextures.Framework.Models.AlternativeTextureModel;
using Object = StardewValley.Object;

namespace AlternativeTextures.Framework.UI;

readonly struct PaintBucketMenuItem()
{
    public readonly required TextureIdentifier TextureIdentifier { get; init; }
    public readonly string? DisplayName
    {
        get;
        init { field = string.IsNullOrWhiteSpace(value) ? null : value; }
    }
}

readonly struct GridSize(int rows, int columns)
{
    public readonly int Rows { get; init; } = rows;
    public readonly int Columns { get; init; } = columns;

    public int Count
    {
        get { return Rows * Columns; }
    }
}

internal class PaintBucketMenu : IClickableMenu
{
    public ClickableComponent? hovered;
    public List<PaintBucketMenuItem> menuItems = [];

    public List<ClickableComponent> itemGrid = [];

    protected string _title;

    protected int _startingRow = 0;
    protected float _buildingScale = 3f;

    GridSize gridSize = new(rows: 4, columns: 6);

    private Dictionary<string, Texture2D> _skinIdToTextures = [];
    private Dictionary<string, Texture2D> _breedIdToTextures = [];

    private void setScrollBarToCurrentIndex()
    {
        if (menuItems.Count > 0)
        {
            var listProgress =
                VirtualRows == 0
                    ? 0
                    : Math.Clamp((float)_startingRow / (VirtualRows - gridSize.Rows), 0, 1);
            // scrollBar.bounds.Y = (int)(num * (float)_startingRow + (float)upArrow.bounds.Bottom + 4f);
            var moveableHeight =
                scrollBarRunner.Height - (scrollBar.bounds.Height * Game1.pixelZoom) - 8;
            scrollBar.bounds.Y = (int)(scrollBarRunner.Top + (moveableHeight * listProgress));
        }
    }

    int VirtualRows
    {
        get { return (int)Math.Ceiling((double)menuItems.Count / gridSize.Columns); }
    }

    ClickableTextureComponent upArrow;
    ClickableTextureComponent downArrow;
    ClickableTextureComponent scrollBar;
    Rectangle scrollBarRunner;

    ShopMenu.ShopCachedTheme VisualTheme = new(null);
    readonly IPaintable target;

    /// IEnumerable<PaintBucketMenuItem> menuItems
    public PaintBucketMenu(
        IPaintable target,
        string uiTitle = "Paint Bucket",
        int textureTileWidth = -1
    )
        : base(0, 0, 832, 576, showUpperRightCloseButton: true)
    {
        this.target = target;
        this._title = uiTitle;
        // this._isSprayCan = isSprayCan;

        var textureType = target.ModelIdentifier.Type;

        // Set up menu structure
        if (
            LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko
            || LocalizedContentManager.CurrentLanguageCode
                == LocalizedContentManager.LanguageCode.fr
        )
        {
            base.height += 64;
        }

        Vector2 topLeft = Utility.getTopLeftPositionForCenteringOnScreen(base.width, base.height);
        base.xPositionOnScreen = (int)topLeft.X;
        base.yPositionOnScreen = (int)topLeft.Y;

        /////////////////////////////////////

        // Populate the texture selection components
        /// TODO Add back itemId (if it is available, but GiantCrops do not have itemId for exampe)
        var availableModels = AlternativeTextures.textureManager.GetAvailableTextureModels(
            "",
            target.ModelIdentifier.ToString(),
            Game1.GetSeasonForLocation(Game1.currentLocation)
        );
        foreach (var model in availableModels)
        {
            var manualVariations = model.ManualVariations.Where(v => v.Id != -1).ToList();
            if (manualVariations.Count > 0)
            {
                foreach (var manualVariation in manualVariations)
                {
                    var menuItem = new PaintBucketMenuItem()
                    {
                        TextureIdentifier = new(model, manualVariation.Id.ToString()),
                        DisplayName = manualVariation.Name,
                    };

                    /// TODO Hoist this up somehow, so it is checked once at the end of this collection
                    // if (AlternativeTextures.modConfig.IsTextureVariationDisabled(objectWithVariation.modData[textureNameKey], manualVariation.Id))
                    // {
                    //     continue;
                    // }

                    /// TODO This should happen inside Paintable.DrawWithTexture()
                    // if (target is Furniture furniture)
                    // {
                    //     (objectWithVariation as Furniture).currentRotation.Value = furniture.currentRotation.Value;
                    //     (objectWithVariation as Furniture).updateRotation();
                    // }

                    this.menuItems.Add(menuItem);
                }
            }
            else
            {
                foreach (var variation in Enumerable.Range(0, model.Variations))
                {
                    var menuItem = new PaintBucketMenuItem()
                    {
                        TextureIdentifier = new(model, variation.ToString()),
                    };
                    this.menuItems.Add(menuItem);
                }
            }
        }

        // Add the vanilla version
        // bool hasHandledVanillaVersion = false;
        // if (textureType is TextureType.Decoration)
        // {
        //     int index = 0;

        //     var allDecorations = ItemQueryResolver.TryResolve(modelName.Contains("Floor") ? "ALL_ITEMS (FL)" : "ALL_ITEMS (WP)", context: null);
        //     foreach (Wallpaper decoration in allDecorations.Where(d => d.Item is Wallpaper wallpaper).Select(d => d.Item as Wallpaper))
        //     {
        //         if (!String.IsNullOrEmpty(decoration.setId.Value))
        //         {
        //             continue;
        //         }

        //         decoration.modData[textureOwnerKey] = AlternativeTextures.DEFAULT_OWNER;
        //         decoration.modData[textureNameKey] = $"{decoration.modData[textureOwnerKey]}.{modelName}";
        //         decoration.modData[textureVariationKey] = decoration.ParentSheetIndex.ToString();
        //         decoration.modData[textureSeasonKey] = String.Empty;

        //         var menuItem = new PaintBucketMenuItem()
        //         {
        //             TextureIdentifier = new(model, manualVariation.Id.ToString()),
        //             DisplayName = manualVariation.Name,
        //         };

        //         this.cachedTextureOptions.Insert(index, decoration);

        //         index++;
        //     }

        //     hasHandledVanillaVersion = true;
        // }
        // else if (textureType is TextureType.Building && PatchTemplate.GetBuildingAt(Game1.currentLocation, (int)position.X, (int)position.Y) is Building building)
        // {
        //     if (building.GetData() is var buildingData && buildingData is not null && buildingData.Skins is not null && buildingData.Skins.Count > 0)
        //     {
        //         foreach (var skin in buildingData.Skins.OrderByDescending(s => s.Id))
        //         {
        //             try
        //             {
        //                 _skinIdToTextures[skin.Id] = AlternativeTextures.modHelper.GameContent.Load<Texture2D>(skin.Texture);

        //                 var buildingInstance = target.getOne();
        //                 buildingInstance.modData[textureOwnerKey] = AlternativeTextures.DEFAULT_OWNER;
        //                 buildingInstance.modData[textureNameKey] = $"{buildingInstance.modData[textureOwnerKey]}.{modelName}";
        //                 buildingInstance.modData[textureDisplayNameKey] = $"{skin.Id}";
        //                 buildingInstance.modData[textureVariationKey] = $"{-1}";
        //                 buildingInstance.modData[textureSeasonKey] = String.Empty;

        //                 this.cachedTextureOptions.Insert(0, buildingInstance);
        //             }
        //             catch (Exception ex)
        //             {
        //                 AlternativeTextures.monitor.Log($"Failed to load building skin for {skin.Id}: {ex}", StardewModdingAPI.LogLevel.Trace);
        //             }
        //         }

        //         if (availableModels.Count == 0)
        //         {
        //             availableModels.Add(new AlternativeTextureModel() { TextureHeight = building.texture.Value.Height, TextureWidth = building.texture.Value.Width, Textures = new Dictionary<int, Texture2D>() { { 0, building.texture.Value } } });
        //         }
        //     }
        // }
        // else if (textureType is TextureType.Character && PatchTemplate.GetCharacterAt(target.Location, (int)position.X, (int)position.Y) is Character character && character is not Horse)
        // {
        //     // Handle vanilla / Content Patcher added skins
        //     if (character is FarmAnimal animal && animal.GetAnimalData() is var animalData && animalData is not null && animalData.Skins is not null)
        //     {
        //         foreach (var skin in animalData.Skins.OrderByDescending(s => s.Id))
        //         {
        //             try
        //             {
        //                 _skinIdToTextures[skin.Id] = AlternativeTextures.modHelper.GameContent.Load<Texture2D>(animal.isBaby() ? skin.BabyTexture : skin.Texture);

        //                 var animalInstance = target.getOne();
        //                 animalInstance.modData[textureOwnerKey] = AlternativeTextures.DEFAULT_OWNER;
        //                 animalInstance.modData[textureNameKey] = $"{skin.Id}";
        //                 animalInstance.modData[textureDisplayNameKey] = $"{skin.Id}";
        //                 animalInstance.modData[textureVariationKey] = $"{-1}";
        //                 animalInstance.modData[textureSeasonKey] = String.Empty;

        //                 this.cachedTextureOptions.Insert(0, animalInstance);
        //                 this.cachedTextureOptions.Insert(0, animalInstance);
        //             }
        //             catch (Exception ex)
        //             {
        //                 AlternativeTextures.monitor.Log($"Failed to load animal skin for {skin.Id}: {ex}", StardewModdingAPI.LogLevel.Trace);
        //             }
        //         }

        //         // Add the vanilla skin (i.e. none)
        //         try
        //         {
        //             var tempSkinId = animal.skinID.Value;
        //             animal.skinID.Value = null;
        //             _skinIdToTextures[AlternativeTextures.DEFAULT_OWNER] = AlternativeTextures.modHelper.GameContent.Load<Texture2D>(animal.GetTexturePath());
        //             animal.skinID.Value = tempSkinId;

        //             var animalInstance = target.getOne();
        //             animalInstance.modData[textureOwnerKey] = AlternativeTextures.DEFAULT_OWNER;
        //             animalInstance.modData[textureNameKey] = $"{AlternativeTextures.DEFAULT_OWNER}";
        //             animalInstance.modData[textureDisplayNameKey] = $"{AlternativeTextures.DEFAULT_OWNER}";
        //             animalInstance.modData[textureVariationKey] = $"{-1}";
        //             animalInstance.modData[textureSeasonKey] = String.Empty;

        //             this.cachedTextureOptions.Insert(0, animalInstance);
        //         }
        //         catch (Exception ex)
        //         {
        //             AlternativeTextures.monitor.Log($"Failed to load default animal skin for {animal.Name}: {ex}", StardewModdingAPI.LogLevel.Trace);
        //         }

        //         if (availableModels.Count == 0)
        //         {
        //             availableModels.Add(new AlternativeTextureModel() { TextureHeight = animal.Sprite.Texture.Height, TextureWidth = animal.Sprite.Texture.Width, Textures = new Dictionary<int, Texture2D>() { { 0, animal.Sprite.Texture } } });
        //         }

        //         hasHandledVanillaVersion = true;
        //     }
        //     else if (character is Pet pet && pet.GetPetData() is var petData && petData is not null && petData.Breeds is not null)
        //     {
        //         foreach (var breed in petData.Breeds.OrderByDescending(b => b.Id))
        //         {
        //             try
        //             {
        //                 _breedIdToTextures[breed.Id] = AlternativeTextures.modHelper.GameContent.Load<Texture2D>(breed.Texture);

        //                 var petInstance = target.getOne();
        //                 petInstance.modData[textureOwnerKey] = AlternativeTextures.DEFAULT_OWNER;
        //                 petInstance.modData[textureNameKey] = $"{breed.Id}";
        //                 petInstance.modData[textureDisplayNameKey] = $"{breed.Id}";
        //                 petInstance.modData[textureVariationKey] = $"{-1}";
        //                 petInstance.modData[textureSeasonKey] = String.Empty;

        //                 this.cachedTextureOptions.Insert(0, petInstance);
        //             }
        //             catch (Exception ex)
        //             {
        //                 AlternativeTextures.monitor.Log($"Failed to load pet breed for {breed.Id}: {ex}", StardewModdingAPI.LogLevel.Trace);
        //             }
        //         }

        //         if (availableModels.Count == 0)
        //         {
        //             availableModels.Add(new AlternativeTextureModel() { TextureHeight = pet.Sprite.Texture.Height, TextureWidth = pet.Sprite.Texture.Width, Textures = new Dictionary<int, Texture2D>() { { 0, pet.Sprite.Texture } } });
        //         }

        //         hasHandledVanillaVersion = true;
        //     }
        // }

        // var vanillaObject = target.getOne();
        // vanillaObject.modData[textureOwnerKey] = AlternativeTextures.DEFAULT_OWNER;
        // vanillaObject.modData[textureNameKey] = $"{vanillaObject.modData[textureOwnerKey]}.{modelName}";
        // vanillaObject.modData[textureDisplayNameKey] = AlternativeTextures.DEFAULT_OWNER;
        // vanillaObject.modData[textureVariationKey] = $"{-1}";
        // vanillaObject.modData[textureSeasonKey] = String.Empty;

        // if (target is Furniture)
        // {
        //     (vanillaObject as Furniture).currentRotation.Value = (target as Furniture).currentRotation.Value;
        //     (vanillaObject as Furniture).updateRotation();
        // }

        this.menuItems.Insert(
            0,
            new() { TextureIdentifier = TextureIdentifier.Default, DisplayName = "Stardew Valley" }
        );

        // _textureType = textureType;

        var drawingScale = 4f;
        var widthOffsetScale = 2;
        var xOffset = 0;
        var _sourceRect = target
            .PreviewTexture(target.Texture ?? TextureIdentifier.Default)
            ?.SourceRect;
        var sourceRect = _sourceRect ?? new Rectangle(0, 0, 0, 0);

        // var sourceRect = SourceRects.GetSourceRectangle(availableModels.First(), target, availableModels.First().TextureWidth, availableModels.First().TextureHeight, -1);
        switch (target.ModelIdentifier.Type)
        {
            case TextureType.Craftable:
                if (sourceRect.Height <= 16)
                {
                    gridSize = gridSize with { Rows = 8 };
                }
                break;
            case TextureType.Flooring:
                sourceRect = new Rectangle(0, 0, 16, 32);
                break;
            case TextureType.Character:
                sourceRect = new Rectangle(0, 0, 32, 32);
                break;
            case TextureType.Tree:
                gridSize = new(rows: 1, columns: 3);
                widthOffsetScale = 4;
                sourceRect = new Rectangle(0, 0, 48, 96);
                break;
            case TextureType.FruitTree:
                gridSize = new(rows: 1, columns: 3);
                widthOffsetScale = 4;
                sourceRect = new Rectangle(0, 0, 48, 80);
                break;
            case TextureType.Crop:
                gridSize = new(rows: 4, columns: 1);
                widthOffsetScale = 4;
                xOffset = 96;
                sourceRect = new Rectangle(0, 0, 128, 32);
                break;
            case TextureType.GiantCrop:
                gridSize = new(rows: 2, columns: 3);
                widthOffsetScale = 4;
                sourceRect = new Rectangle(0, 0, 48, 64);
                break;
            case TextureType.Grass:
                gridSize = new(rows: 6, columns: 4);
                widthOffsetScale = 3;
                xOffset = 32;
                sourceRect = new Rectangle(0, 0, 15, 20);
                break;
            case TextureType.Bush:
                gridSize = new(rows: 6, columns: 4);
                widthOffsetScale = 3;
                xOffset = 32;
                break;
            case TextureType.Furniture:
                if (sourceRect.Height >= 64)
                {
                    gridSize = gridSize with { Rows = 2 };
                }
                else if (sourceRect.Height >= 32)
                {
                    gridSize = gridSize with { Rows = 3 };
                }
                else if (sourceRect.Height <= 16)
                {
                    sourceRect.Height = 32;
                }

                break;
            case TextureType.Building:
                gridSize = new(rows: 1, columns: 3);
                widthOffsetScale = 4;
                sourceRect = new Rectangle(0, 0, 48, 160);

                switch (textureTileWidth)
                {
                    case int w when w > 4 && w < 8:
                        _buildingScale = 2f;
                        break;
                    case int w when w >= 8:
                        _buildingScale = 1f;
                        break;
                }

                drawingScale = _buildingScale;
                break;
            case TextureType.Decoration:
                widthOffsetScale = 3;
                gridSize = new(rows: 2, columns: 4);
                sourceRect = new Rectangle(0, 0, 32, 64);
                break;
        }

        var buttonWidth = width / gridSize.Columns;
        var buttonHeight = height / gridSize.Rows;
        if (availableModels.FirstOrDefault() is { } firstModel)
        {
            foreach (var row in Enumerable.Range(0, gridSize.Rows))
            {
                foreach (var column in Enumerable.Range(0, gridSize.Columns))
                {
                    var componentId = column + (row * gridSize.Columns);
                    // var weirdRow = componentId % gridSize.Columns;
                    var weirdRow = row;
                    this.itemGrid.Add(
                        new ClickableComponent(
                            new Rectangle(
                                // base.xPositionOnScreen + IClickableMenu.borderWidth + componentId % gridSize.Columns * 64 * widthOffsetScale + xOffset,
                                // base.yPositionOnScreen + sourceRect.Height + componentId / gridSize.Columns * (4 * sourceRect.Height),

                                xPositionOnScreen + (buttonWidth * column),
                                yPositionOnScreen + (buttonWidth * row),
                                buttonWidth,
                                buttonHeight
                            ),
                            ""
                        )
                        {
                            myID = componentId,
                            downNeighborID = componentId + gridSize.Columns,
                            upNeighborID = row >= 0 ? componentId - gridSize.Columns : -1,
                            rightNeighborID = column == 5 ? 9997 : componentId + 1,
                            leftNeighborID = column > 0 ? componentId - 1 : 9998,
                        }
                    );
                }
            }
        }

        /////////////////////////////////////

        upArrow = new ClickableTextureComponent(
            new Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen + 16, 44, 48),
            VisualTheme.ScrollUpTexture,
            VisualTheme.ScrollUpSourceRect,
            4f
        )
        {
            myID = 97865,
            downNeighborID = 106,
            leftNeighborID = 3546,
        };
        downArrow = new ClickableTextureComponent(
            new Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen + height - 64, 44, 48),
            VisualTheme.ScrollDownTexture,
            VisualTheme.ScrollDownSourceRect,
            4f
        )
        {
            myID = 106,
            upNeighborID = 97865,
            leftNeighborID = 3546,
        };

        var scrollbarHeight = height - 64 - upArrow.bounds.Height - 28;
        scrollBar = new ClickableTextureComponent(
            new Rectangle(
                upArrow.bounds.X + 12,
                upArrow.bounds.Y + upArrow.bounds.Height + 4,
                24,
                scrollbarHeight / Math.Max(1, VirtualRows)
            // 40
            ),
            VisualTheme.ScrollBarFrontTexture,
            VisualTheme.ScrollBarFrontSourceRect,
            4f
        );
        scrollBarRunner = new Rectangle(
            scrollBar.bounds.X,
            upArrow.bounds.Bottom + 4,
            scrollBar.bounds.Width,
            this.height - 64 - upArrow.bounds.Height - 28
        );

        ////////////////////////////////////////////

        // Call snap functions
        if (Game1.options.SnappyMenus)
        {
            base.populateClickableComponentList();
            this.setCurrentlySnappedComponentTo(0);
            this.snapCursorToCurrentSnappedComponent();
        }
    }

    protected override void customSnapBehavior(int direction, int oldRegion, int oldID)
    {
        base.customSnapBehavior(direction, oldRegion, oldID);
    }

    public override void performHoverAction(int x, int y)
    {
        this.hovered = null;
        if (Game1.IsFading())
        {
            return;
        }

        var maxScale = target.ModelIdentifier.Type == TextureType.Building ? _buildingScale : 4f;
        foreach (var button in this.itemGrid)
        {
            if (button.containsPoint(x, y))
            {
                this.hovered = button;
            }
        }
    }

    public override void receiveKeyPress(Keys key)
    {
        PrettyPrint.Log("KeyPress", key);
        // if (key == Keys.Escape)
        // {
        //     base.receiveKeyPress(key);
        // }

        base.receiveKeyPress(key);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = false)
    {
        base.receiveLeftClick(x, y, playSound);
        if (Game1.activeClickableMenu == null)
        {
            return;
        }

        foreach (var (button, item) in elementsOnScreen)
        {
            if (!button.containsPoint(x, y))
                continue;

            Console.Log($"item: {item}");
            target.ApplyTexture(item.TextureIdentifier);

            // if (_textureType is TextureType.Character && PatchTemplate.GetCharacterAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Character character && character != null)
            // {
            //     foreach (string key in c.item.modData.Keys)
            //     {
            //         character.modData[key] = c.item.modData[key];
            //     }

            //     if (character is FarmAnimal animal && _skinIdToTextures.ContainsKey(character.modData[_textureDisplayNameKey]))
            //     {
            //         animal.skinID.Value = animal.modData[_textureDisplayNameKey];
            //     }
            //     else if (character is Pet pet && _breedIdToTextures.ContainsKey(character.modData[_textureDisplayNameKey]))
            //     {
            //         pet.whichBreed.Value = pet.modData[_textureDisplayNameKey];
            //     }
            // }
            // else if (PatchTemplate.GetObjectAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) != null)
            // {
            //     foreach (string key in c.item.modData.Keys)
            //     {
            //         _textureTarget.modData[key] = c.item.modData[key];
            //     }
            // }
            // else if (PatchTemplate.GetResourceClumpAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is GiantCrop giantCrop)
            // {
            //     foreach (string key in c.item.modData.Keys)
            //     {
            //         giantCrop.modData[key] = c.item.modData[key];
            //     }
            // }
            // else if (PatchTemplate.GetBuildingAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Building building)
            // {
            //     foreach (string key in c.item.modData.Keys)
            //     {
            //         building.modData[key] = c.item.modData[key];
            //     }

            //     if (_skinIdToTextures.ContainsKey(building.modData[_textureDisplayNameKey]))
            //     {
            //         building.skinId.Value = building.modData[_textureDisplayNameKey];
            //     }
            //     else
            //     {
            //         building.skinId.Value = null;
            //     }

            //     building.resetTexture();
            //     AlternativeTextures.messageManager.SendBuildingTextureUpdate(building);

            //     if (building is ShippingBin shippingBin && shippingBin.modData[_textureOwnerKey] == AlternativeTextures.DEFAULT_OWNER)
            //     {
            //         shippingBin.initLid();
            //     }
            // }
            // else if (Game1.currentLocation is Farm mailBoxFarm && mailBoxFarm.GetMainMailboxPosition() is Point mailboxPosition && PatchTemplate.IsPositionNearMailbox(Game1.currentLocation, mailboxPosition, (int)(_position.X / 64), (int)(_position.Y / 64)))
            // {
            //     foreach (string key in c.item.modData.Keys)
            //     {
            //         Game1.currentLocation.modData[key] = c.item.modData[key];
            //     }

            //     var farmerHouse = mailBoxFarm.GetMainFarmHouse();
            //     if (farmerHouse.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) is false)
            //     {
            //         var instanceSeasonName = $"{TextureType.Building}_{$"Farmhouse_{Game1.MasterPlayer.HouseUpgradeLevel}"}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
            //         PatchTemplate.AssignDefaultModData(farmerHouse, instanceSeasonName, true);
            //     }
            // }
            // else if (PatchTemplate.GetTerrainFeatureAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is TerrainFeature feature)
            // {
            //     foreach (string key in c.item.modData.Keys)
            //     {
            //         Console.Log($"[{key}] {c.item.modData[key]}");
            //         feature.modData[key] = c.item.modData[key];
            //     }
            // }
            // else if (Game1.currentLocation is DecoratableLocation decoratableLocation && (string.IsNullOrEmpty(decoratableLocation.GetFloorID((int)_position.X, (int)_position.Y)) is false || string.IsNullOrEmpty(decoratableLocation.GetWallpaperID((int)_position.X, (int)_position.Y)) is false))
            // {
            //     string room;
            //     var isFloor = _modelName.Contains("Floor");
            //     if (isFloor)
            //     {
            //         room = decoratableLocation.GetFloorID((int)_position.X, (int)_position.Y);
            //     }
            //     else
            //     {
            //         room = decoratableLocation.GetWallpaperID((int)_position.X, (int)_position.Y);
            //     }

            //     if (string.IsNullOrEmpty(room) is false)
            //     {
            //         int variation = Int32.Parse(c.item.modData[_textureVariationKey]);
            //         var decorationKey = c.item.modData[_textureOwnerKey] == AlternativeTextures.DEFAULT_OWNER ? variation.ToString() : $"{c.item.modData[_textureNameKey]}:{variation}";
            //         if (isFloor)
            //         {
            //             if (variation == -1)
            //             {
            //                 decorationKey = decoratableLocation.GetFirstFlooringTile().ToString();
            //             }
            //             decoratableLocation.SetFloor(decorationKey, room);
            //         }
            //         else
            //         {
            //             if (variation == -1)
            //             {
            //                 decorationKey = "0";
            //             }
            //             decoratableLocation.SetWallpaper(decorationKey, room);
            //         }
            //     }
            // }

            /// TODO Bring coloring animation back, but through IPaintable (or an optional interface)
            // Draw coloring animation
            // for (int j = 0; j < 12; j++)
            // {
            //     var randomColor = new Color(Game1.random.Next(256), Game1.random.Next(256), Game1.random.Next(256));
            //     AlternativeTextures.multiplayer.broadcastSprites(Game1.currentLocation, new TemporaryAnimatedSprite(6, _textureTarget.TileLocation * 64f, randomColor, 8, flipped: false, 50f)
            //     {
            //         motion = new Vector2((float)Game1.random.Next(-10, 11) / 10f, -Game1.random.Next(1, 3)),
            //         acceleration = new Vector2(0f, (float)Game1.random.Next(1, 3) / 100f),
            //         accelerationChange = new Vector2(0f, -0.001f),
            //         scale = 0.8f,
            //         layerDepth = (_textureTarget.TileLocation.Y + 1f) * 64f / 10000f,
            //         interval = Game1.random.Next(20, 90)
            //     });
            // }

            //Game1.player.currentLocation.localSound("crit");
            this.exitThisMenu();
            return;
        }

        if (
            downArrow.containsPoint(x, y)
            && _startingRow < Math.Max(0, VirtualRows - gridSize.Rows)
        )
        {
            downArrowPressed();
            Game1.playSound("shwip");
        }
        else if (upArrow.containsPoint(x, y) && _startingRow > 0)
        {
            upArrowPressed();
            Game1.playSound("shwip");
        }
        else if (scrollBar.containsPoint(x, y))
        {
            Console.Log($"LeftClickDown on scrollbar....");
            scrolling = true;
        }
        else if (
            !downArrow.containsPoint(x, y)
            && x > xPositionOnScreen + width
            && x < xPositionOnScreen + width + 128
            && y > yPositionOnScreen
            && y < yPositionOnScreen + height
        )
        {
            Console.Log($"This odd case");
            scrolling = true;
            leftClickHeld(x, y);
            releaseLeftClick(x, y);
        }
    }

    private bool scrolling = false;

    private void downArrowPressed()
    {
        downArrow.scale = downArrow.baseScale;
        _startingRow++;
        setScrollBarToCurrentIndex();
        // updateSaleButtonNeighbors();
    }

    private void upArrowPressed()
    {
        upArrow.scale = upArrow.baseScale;
        _startingRow--;
        setScrollBarToCurrentIndex();
        // updateSaleButtonNeighbors();
    }

    public override void leftClickHeld(int x, int y)
    {
        base.leftClickHeld(x, y);
        Console.Log($"leftClickHeld.... {scrolling}");
        if (scrolling)
        {
            int y2 = scrollBar.bounds.Y;
            scrollBar.bounds.Y = Math.Min(
                yPositionOnScreen + height - 64 - 12 - scrollBar.bounds.Height,
                Math.Max(y, yPositionOnScreen + upArrow.bounds.Height + 20)
            );
            float num = (float)(y - scrollBarRunner.Y) / (float)scrollBarRunner.Height;
            _startingRow = Math.Min(
                Math.Max(0, VirtualRows - gridSize.Rows),
                Math.Max(0, (int)((float)VirtualRows * num))
            );
            setScrollBarToCurrentIndex();
            //   updateSaleButtonNeighbors();
            if (y2 != scrollBar.bounds.Y)
            {
                Game1.playSound("shiny4");
            }
        }
    }

    public override void releaseLeftClick(int x, int y)
    {
        base.releaseLeftClick(x, y);
        Console.Log($"Release left click....");
        scrolling = false;
    }

    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);
        if (direction > 0 && _startingRow > 0)
        {
            _startingRow--;
            Game1.playSound("shiny4");
        }
        else if (direction < 0 && (gridSize.Rows + _startingRow) < VirtualRows)
        {
            _startingRow++;
            Game1.playSound("shiny4");
        }

        setScrollBarToCurrentIndex();
    }

    IEnumerable<(ClickableComponent, PaintBucketMenuItem)> elementsOnScreen
    {
        get
        {
            var pageOffset = _startingRow * gridSize.Columns;
            return Enumerable.Zip(this.itemGrid, this.menuItems.Skip(pageOffset));
        }
    }

    public override void draw(SpriteBatch batch)
    {
        if (!Game1.dialogueUp && !Game1.IsFading())
        {
            batch.Draw(
                Game1.fadeToBlackRect,
                Game1.graphics.GraphicsDevice.Viewport.Bounds,
                Color.Black * 0.75f
            );
            SpriteText.drawStringWithScrollCenteredAt(
                batch,
                _title,
                base.xPositionOnScreen + base.width / 4,
                base.yPositionOnScreen - 64
            );
            IClickableMenu.drawTextureBox(
                batch,
                Game1.mouseCursors,
                new Rectangle(384, 373, 18, 18),
                base.xPositionOnScreen,
                base.yPositionOnScreen,
                base.width,
                base.height,
                Color.White,
                4f
            );

            foreach (var (button, option) in elementsOnScreen)
            {
                IClickableMenu.drawTextureBox(
                    batch,
                    VisualTheme.ItemRowBackgroundTexture,
                    VisualTheme.ItemRowBackgroundSourceRect,
                    button.bounds.X,
                    button.bounds.Y,
                    button.bounds.Width,
                    button.bounds.Height,
                    (button.containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY()) && !scrolling)
                        ? VisualTheme.ItemRowBackgroundHoverColor
                        : Color.White,
                    4f,
                    drawShadow: false
                );

                if (target.PreviewTexture(option.TextureIdentifier) is { } drawableTexture)
                {
                    batch.Draw(
                        drawableTexture.Texture,
                        button.bounds,
                        drawableTexture.SourceRect,
                        Color.White,
                        0f,
                        new Vector2(0, 0),
                        SpriteEffects.None,
                        0f
                    );
                }
                else
                {
                    /// Draw error texture?
                }
                // button.draw(batch, Color.White, 0.87f);
            }

            if (menuItems.Count > gridSize.Count)
            {
                upArrow.draw(batch);
                downArrow.draw(batch);
                IClickableMenu.drawTextureBox(
                    batch,
                    VisualTheme.ScrollBarBackTexture,
                    VisualTheme.ScrollBarBackSourceRect,
                    scrollBarRunner.X,
                    scrollBarRunner.Y,
                    scrollBarRunner.Width,
                    scrollBarRunner.Height,
                    Color.White,
                    4f
                );
                scrollBar.draw(batch);
            }
        }

        /// TODO Hover
        // var hoverInfoText = String.Empty;
        // var hoverDisplayName = "Hover over an item to see its texture name!";
        // if (this.hovered != null && this.hovered.item != null)
        // {
        //     if (this.hovered.item.modData.ContainsKey(_textureOwnerKey) && this.hovered.item.modData.ContainsKey(_textureVariationKey))
        //     {
        //         if (this.hovered.item.modData.ContainsKey(_textureDisplayNameKey) && !String.IsNullOrEmpty(this.hovered.item.modData[_textureDisplayNameKey]))
        //         {
        //             hoverInfoText = String.Concat(this.hovered.item.modData[_textureOwnerKey], " > ", Int32.Parse(this.hovered.item.modData[_textureVariationKey]) + 1);
        //             hoverDisplayName = this.hovered.item.modData[_textureDisplayNameKey];
        //         }
        //         else
        //         {
        //             hoverDisplayName = String.Concat(this.hovered.item.modData[_textureOwnerKey], " > ", Int32.Parse(this.hovered.item.modData[_textureVariationKey]) + 1);
        //         }
        //     }
        // }
        // SpriteText.drawStringWithScrollCenteredAt(batch hoverDisplayName, Game1.uiViewport.Width / 2, base.yPositionOnScreen + base.height + 16, "Hover over an item to see its texture name!");

        // if (!String.IsNullOrEmpty(hoverInfoText))
        // {
        //     IClickableMenu.drawHoverText(batch hoverInfoText, Game1.smallFont);
        // }

        Game1.mouseCursorTransparency = 1f;
        base.drawMouse(batch);
    }
}
