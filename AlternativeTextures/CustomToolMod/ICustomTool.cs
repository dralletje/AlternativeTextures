using System;
using System.Collections.Generic;
using StardewModdingAPI.Events;

namespace AlternativeTextures.CustomToolMod;

public interface ICustomTool
{
    public IDisposable? Start();

    public IEnumerator<bool>? OnButton(ButtonPressedEventArgs e) => null;
}
