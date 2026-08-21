using System;
using System.Collections.Generic;
using StardewModdingAPI.Events;

namespace AlternativeTextures.App.Tools;

public interface ICustomTool
{
    public IDisposable? Start();

    public IEnumerator<bool>? OnButton(ButtonPressedEventArgs e) => null;
}
