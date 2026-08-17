// using System.Collections.Generic;
// using StardewModdingAPI;

// namespace AlternativeTextures;

// interface IButtonRoutine
// {
  
// }

// public class ButtonContext(SButton initialButton, ICursorPosition cursor)
// {
//   public readonly SButton InitialButton = initialButton;
//   public ICursorPosition Cursor = cursor;
//   public ButtonEvent LastEvent = new ButtonEvent.KeyPress(initialButton);
// }

// public class ButtonRoutine(SButton initialButton, ICursorPosition cursor)
// {
//   ButtonContext context = new ButtonContext(initialButton, cursor);
//   IEnumerator<ButtonResponse> = 

//   public progress()
//   {
    
//   }
// }

// public abstract record ButtonEvent
// {
//   private ButtonEvent() { }

//   public sealed record Tick : ButtonEvent;
//   public sealed record KeyPress(SButton Button) : ButtonEvent;
//   public sealed record KeyRelease(SButton Button) : ButtonEvent;
// }

// public abstract record ButtonEndEvent
// {
//   private ButtonEndEvent() { }

//   public sealed record Tick : ButtonEndEvent;
//   public sealed record KeyPress(SButton Button) : ButtonEndEvent;
//   public sealed record KeyRelease(SButton Button) : ButtonEndEvent;
// }


// public abstract record ButtonResponse
// {
//   private ButtonResponse() { }

//   public sealed record Continue : ButtonResponse;
//   public sealed record Handled : ButtonResponse;
//   public sealed record Ignored : ButtonResponse;
// }
