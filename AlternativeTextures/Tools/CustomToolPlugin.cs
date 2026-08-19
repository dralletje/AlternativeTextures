using System;
using System.Collections.Generic;
using ConsoleLog;
using Incubator;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AlternativeTextures.Tools;

public class CustomToolPlugin(IModHelper helper)
{
    public IDisposable Start()
    {
        helper.Events.GameLoop.UpdateTicked += OnTickUpdateCurrentTool;

        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Input.ButtonReleased += OnButtonReleased;
        return new ActionDisposable(() =>
        {
            helper.Events.GameLoop.UpdateTicked -= OnTickUpdateCurrentTool;

            helper.Events.Input.ButtonPressed -= OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            helper.Events.Input.ButtonReleased -= OnButtonReleased;
        });
    }

    public ICustomTool? Current
    {
        get { return currentCustomToolCache.Tool; }
    }

    record CustomToolCache
    {
        public Item? Item { get; init; }
        public ICustomTool? Tool { get; init; }
        public IDisposable? Disposable { get; init; }
    }

    private CustomToolCache currentCustomToolCache = new();

    private void OnTickUpdateCurrentTool(object? sender, UpdateTickedEventArgs e)
    {
        if (Game1.player.CurrentItem != this.currentCustomToolCache.Item)
        {
            if (this.currentCustomToolCache.Disposable is { } disposable)
            {
                disposable.Dispose();
            }

            var currentTool = Game1.player.CurrentTool;
            var nextCustomTool =
                PaintBrushEmptyTool.From(helper, currentTool) as ICustomTool
                ?? PaintBrushFilledTool.From(helper, currentTool) as ICustomTool
                ?? SprayCanTool.From(helper, currentTool) as ICustomTool;

            currentCustomToolCache = new()
            {
                Item = currentTool,
                Tool = nextCustomTool,
                Disposable = nextCustomTool?.Start(),
            };
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

        if (this.Current is { } tool)
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
                    PrettyPrint.Log("Explicit release on start");
                    routine.Dispose();
                }
            }
        }
    }

    private void OnButtonReleased(object? sender, ButtonReleasedEventArgs e)
    {
        if (this.Current is { } tool)
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
        if (this.Current is { } tool)
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
