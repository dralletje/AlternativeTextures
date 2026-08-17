using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

public class ToolTileChange
{
    private readonly IModHelper _helper;
    private readonly SButton _triggerButton;
    private Vector2 _lastPlayerTile;
    private Vector2 _lastMouseTile;

    public event Action<Vector2> OnTileActivated;

    public ToolTileChange(IModHelper helper)
    {
        _helper = helper;
        _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsPlayerFree) return;

        Vector2 playerTile = Game1.player.Tile;
        Vector2 mouseTile = _helper.Input.GetCursorPosition().GrabTile;

        bool tileChanged = playerTile != _lastPlayerTile || mouseTile != _lastMouseTile;
        
        _lastPlayerTile = playerTile;
        _lastMouseTile = mouseTile;

        if (tileChanged && _helper.Input.IsDown(_triggerButton))
        {
            OnTileActivated?.Invoke(mouseTile);
        }
    }
}