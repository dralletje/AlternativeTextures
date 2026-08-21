using System;
using AlternativeTextures.Framework.Managers;
using AlternativeTextures.MetaFramework;
using AlternativeTextures.PatchDrawMod.Patches;
using AlternativeTextures.PatchDrawMod.Patches.Buildings;
using AlternativeTextures.PatchDrawMod.Patches.Entities;
using AlternativeTextures.PatchDrawMod.Patches.GameLocations;
using AlternativeTextures.PatchDrawMod.Patches.SpecialObjects;
using AlternativeTextures.PatchDrawMod.Patches.StandardObjects;
using AlternativeTextures.PatchDrawMod.Patches.Tools;
using HarmonyLib;
using Incubator;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace AlternativeTextures.PatchDrawMod;

class PatchDrawMod<TParent>(DralModContext<TParent, PatchDrawMod<TParent>> context) : DralMod
    where TParent : HasMod<TextureManager>
{
    IMonitor Monitor = context.Monitor;
    IModHelper Helper = context.Helper;
    IManifest ModManifest = context.ModManifest;

    MessageManager messageManager = new(context.Helper, context.ModManifest.UniqueID);

    public override IDisposable? Entry()
    {
        try
        {
            var harmony = new Harmony(ModManifest.UniqueID);

            // Apply texture override related patches
            new GameLocationPatch(Helper).Apply(harmony);
            new ObjectPatch(Helper).Apply(harmony);
            new FencePatch(Helper).Apply(harmony);
            new CropPatch(Helper).Apply(harmony);
            new GiantCropPatch(Helper).Apply(harmony);
            new GrassPatch(Helper).Apply(harmony);
            new TreePatch(Helper).Apply(harmony);
            new FruitTreePatch(Helper).Apply(harmony);
            new ResourceClumpPatch(Helper).Apply(harmony);
            new BushPatch(Helper).Apply(harmony);
            new FlooringPatch(Helper).Apply(harmony);
            new FurniturePatch(Helper).Apply(harmony);
            new BedFurniturePatch(Helper).Apply(harmony);
            new FishTankFurniturePatch(Helper).Apply(harmony);

            // Start of special objects
            new ChestPatch(Helper).Apply(harmony);
            new CrabPotPatch(Helper).Apply(harmony);
            new IndoorPotPatch(Helper).Apply(harmony);
            new PhonePatch(Helper).Apply(harmony);
            new TorchPatch(Helper).Apply(harmony);
            new WoodChipperPatch(Helper).Apply(harmony);

            // Start of entity patches
            new CharacterPatch(Helper).Apply(harmony);
            new FarmAnimalPatch(Helper).Apply(harmony);
            new HorsePatch(Helper).Apply(harmony);
            new PetPatch(Helper).Apply(harmony);
            new MonsterPatch(Helper).Apply(harmony);

            // Start of building patches
            new BuildingPatch(Helper).Apply(harmony);
            new ShippingBinPatch(Helper).Apply(harmony);

            // Start of location patches
            new GameLocationPatch(Helper).Apply(harmony);

            // Paint tool related patches
            new ToolPatch(Helper).Apply(harmony);
        }
        catch (Exception e)
        {
            Monitor.Log($"Issue with Harmony patching: {e}", LogLevel.Error);
        }

        Helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;

        return new ActionDisposable(() =>
        {
            Helper.Events.Multiplayer.ModMessageReceived -= OnModMessageReceived;
        });
    }

    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID == ModManifest.UniqueID)
        {
            messageManager.HandleIncomingMessage(e);
        }
    }
}
