using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;

namespace AlternativeTextures.CustomToolMod;

public interface ICustomTool
{
    public IDisposable? Start();

    public bool DrawMouseCursor(SpriteBatch spriteBatch)
    {
        return false;
    }

    public IEnumerator<bool>? OnButton(ButtonPressedEventArgs e) => null;
}
