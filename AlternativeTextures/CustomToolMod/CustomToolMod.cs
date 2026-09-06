using System;
using System.Collections.Generic;
using AlternativeTextures.MetaFramework;
using AlternativeTextures.Stardew;
using ConsoleLog;
using HarmonyLib;
using Incubator;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;

namespace AlternativeTextures.CustomToolMod;

interface ICustomToolMod
{
    public void Register(string toolId, Func<GenericTool, ICustomTool> factory);
}

record CustomToolCache
{
    public Item? Item { get; init; }
    public ICustomTool? Tool { get; init; }
    public IDisposable? Disposable { get; init; }
}

/// This sucks, but not sure there is another way
/// to get stuff "on to" the patches
static class CustomToolGlobal
{
    static internal CustomToolCache currentCustomToolCache = new();
    static public ICustomTool? Current
    {
        get { return currentCustomToolCache.Tool; }
    }
}

public class CustomToolMod<TParent>(DralModContext<TParent, CustomToolMod<TParent>> context) : DralMod, ICustomToolMod
{
    IModHelper Helper = context.Helper;
    IMonitor Monitor = context.Monitor;

    private Dictionary<string, Func<GenericTool, ICustomTool>> registeredTools = [];

    public void Register(string toolId, Func<GenericTool, ICustomTool> factory)
    {
        registeredTools[toolId] = factory;
    }

    public override IDisposable? Entry()
    {
        var harmony = new Harmony(context.ModManifest.UniqueID);
        harmony.CreateClassProcessor(typeof(DrawMouseCursorPatch)).Patch();

        Helper.Events.GameLoop.UpdateTicked += OnTickUpdateCurrentTool;

        Helper.Events.Input.ButtonPressed += OnButtonPressed;
        Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        Helper.Events.Input.ButtonReleased += OnButtonReleased;

        return new ActionDisposable(() =>
        {
            Helper.Events.GameLoop.UpdateTicked -= OnTickUpdateCurrentTool;

            Helper.Events.Input.ButtonPressed -= OnButtonPressed;
            Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            Helper.Events.Input.ButtonReleased -= OnButtonReleased;
        });
    }

    static public ICustomTool? Current => CustomToolGlobal.Current;

    private void OnTickUpdateCurrentTool(object? sender, UpdateTickedEventArgs e)
    {
        if (Game1.player.CurrentItem != CustomToolGlobal.currentCustomToolCache.Item)
        {
            if (CustomToolGlobal.currentCustomToolCache.Disposable is { } disposable)
            {
                disposable.Dispose();
            }

            if (
                Game1.player.CurrentTool is GenericTool currentTool
                && registeredTools.GetValueOrNull(currentTool.QualifiedItemId) is { } toolFactory
            )
            {
                var nextCustomTool = toolFactory(currentTool);
                CustomToolGlobal.currentCustomToolCache = new()
                {
                    Item = currentTool,
                    Tool = nextCustomTool,
                    Disposable = nextCustomTool?.Start(),
                };
            }
            else
            {
                CustomToolGlobal.currentCustomToolCache = new()
                {
                    Item = Game1.player.CurrentItem,
                    Tool = null,
                    Disposable = null,
                };
            }
        }
    }

    //////////////////////////////////////

    record PressRoutine(ICustomTool Tool, SButton Button, IEnumerator<bool> Routine) { }

    private PressRoutine? currentPressRoutine;

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsPlayerFree)
            return;
        if (e.IsMenuPress)
            return;

        Console.Log($"[OnButtonPressed] {e.Button}");

        if (Current is { } tool)
        {
            if (tool.OnButton(e) is { } routine && routine.MoveNext())
            {
                // Helper.Input.Suppress(e.Button);

                if (routine.Current)
                {
                    /// Wants to continue receiving events
                    this.currentPressRoutine = new PressRoutine(tool, e.Button, routine);
                }
                else
                {
                    /// Is fine with what it has, Dispose just in case
                    Console.Log($"Explicit release on start");
                    routine.Dispose();
                }
            }
        }
    }

    private void OnButtonReleased(object? sender, ButtonReleasedEventArgs e)
    {
        if (Current is { } tool)
        {
            if (this.currentPressRoutine is { } routine && routine.Button == e.Button && routine.Tool == tool)
            {
                Console.Log($"DISPOSE RELEASE: {e.Button.ToString()}");
                routine.Routine.Dispose();
                this.currentPressRoutine = null;
            }
        }
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Current is { } tool)
        {
            if (this.currentPressRoutine is { } routine)
            {
                if (routine.Routine.MoveNext())
                {
                    if (routine.Routine.Current)
                    {
                        /// Wants next events too, no change
                    }
                    else
                    {
                        Console.Log($"Hmmm, dispose in OnUpdateTicked");
                        routine.Routine.Dispose();
                        this.currentPressRoutine = null;
                    }
                }
                else
                {
                    this.currentPressRoutine = null;
                }
            }
        }
    }
}
