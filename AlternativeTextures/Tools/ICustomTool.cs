using System;
using System.Collections.Generic;
using StardewModdingAPI.Events;
using StardewValley;

namespace AlternativeTextures.Tools;

public interface ICustomTool
{
  public IDisposable? Start();

  public IEnumerator<bool>? OnButton(ButtonPressedEventArgs e)
  {
    return null;
  }
}

