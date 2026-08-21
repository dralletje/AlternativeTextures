using System;
using AlternativeTextures.App.Tools;
using AlternativeTextures.CustomToolMod;
using AlternativeTextures.MetaFramework;
using HarmonyLib;
using Incubator;
using StardewModdingAPI;

namespace AlternativeTextures.App;

class App<TParent>(DralModContext<TParent, App<TParent>> context) : DralMod
    where TParent : HasMod<ICustomToolMod>
{
    IMonitor Monitor = context.Monitor;
    IModHelper Helper = context.Helper;
    IManifest ModManifest = context.ModManifest;

    public override IDisposable? Entry()
    {
        try
        {
            var harmony = new Harmony(ModManifest.UniqueID);
            // Paint tool related patches
            new ToolPatch(Helper).Apply(harmony);
        }
        catch (Exception e)
        {
            Monitor.Log($"Issue with Harmony patching: {e}", LogLevel.Error);
        }

        var customToolMod = context.Parent.GetMod<ICustomToolMod>();

        customToolMod.Register(
            AlternativeTextures.PAINT_BRUSH_EMPTY_ID,
            (tool) => new PaintBrushEmptyTool(Helper, tool)
        );
        customToolMod.Register(
            AlternativeTextures.PAINT_BRUSH_FILLED_ID,
            (tool) => new PaintBrushFilledTool(Helper, tool)
        );
        customToolMod.Register(AlternativeTextures.TOOL_ID_PAINT_BUCKET, (tool) => new PaintBucketTool(Helper, tool));

        // PaintBrushEmptyTool.From(Helper, currentTool) as ICustomTool
        //     ?? PaintBrushFilledTool.From(Helper, currentTool) as ICustomTool
        //     ?? SprayCanTool.From(Helper, currentTool) as ICustomTool;

        return new ActionDisposable(() => { });
    }
}
