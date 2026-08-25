using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ConsoleLog.Strings;

public readonly ref struct ActionDisposableStruct(Action? a) : IDisposable
{
    public void Dispose()
    {
        if (a is not null)
        {
            a();
        }
    }
}

record Collector<T>
{
    public List<T> Values = [];

    public void operator +=(T value) => Values.Add(value);
}

public interface ITextComponent
{
    void RenderText(ref TextBuilder Text);
}

abstract record InterpolatedStringPart
{
    public record Literal(String String) : InterpolatedStringPart;

    public record InterpolatedString(String String) : InterpolatedStringPart;

    public record StringComponent(ITextComponent Component) : InterpolatedStringPart;
}

[InterpolatedStringHandler]
public ref struct StringComponentInterpolatedStringHandler(int literalLength, int formattedCount)
{
    internal List<InterpolatedStringPart> Parts = [];

    public void AppendLiteral(string str)
    {
        Parts.Add(new InterpolatedStringPart.Literal(str));
    }

    public void AppendFormatted(string str)
    {
        Parts.Add(new InterpolatedStringPart.InterpolatedString(str));
    }

    public void AppendFormatted(ITextComponent value)
    {
        Parts.Add(new InterpolatedStringPart.StringComponent(value));
    }
}

/// TODO Turns into `ref struct` when I got rid of all lambda based uses
public ref struct TextBuilder(StringBuilder stringBuilder, string indentUnit = "    ") : IDisposable
{
    public static string Render(ITextComponent component)
    {
        var stringBuilder = new StringBuilder();
        var Text = new TextBuilder(stringBuilder);
        Text += component;
        return stringBuilder.ToString();
    }

    public record Result
    {
        public required List<string> Strings { get; init; }
        // public required List<Func<Point, bool>> LeftClickHandlers { get; init; }
        // public required List<Action<bool>> UpdateHandlers { get; init; }
        // public required List<Action<GameTime>> TickHandlers { get; init; }
        // public required List<Action<Direction>> MovementKeyHandlers { get; init; }
        // public required List<Func<Point, bool>> HoverHandlers { get; init; }
    }

    // Collector<Func<Point, bool>> OnLeftClick = new();
    // Collector<Func<Point, bool>> OnHover = new();
    // Collector<Action<bool>> OnUpdate = new();
    // Collector<Action<GameTime>> OnTick = new();
    // Collector<Action<Direction>> OnMovementKey = new();

    /// Until I have everything "ref-ed up", I'm going to save some stuff in a reference type
    record Layer()
    {
        public object Identity { get; } = new();
        public required int Identation;
    }

    Stack<Layer> Layers = new([new Layer() { Identation = 0 }]);

    Layer Current => Layers.Peek();
    int Indentation => Current.Identation;

    bool OnNewLine = false;

    public void operator +=(ITextComponent component)
    {
        if (OnNewLine is true)
        {
            OnNewLine = false;
            stringBuilder.Append("\n");
            for (var i = 0; i < Indentation; i++)
            {
                stringBuilder.Append(indentUnit);
            }
        }
        component.RenderText(ref this);
        OnNewLine = true;
    }

    public void operator +=(string str)
    {
        if (OnNewLine is true)
        {
            OnNewLine = false;
            stringBuilder.Append("\n");
            for (var i = 0; i < Indentation; i++)
            {
                stringBuilder.Append(indentUnit);
            }
        }
        stringBuilder.Append(str);
        OnNewLine = true;
    }

    public void operator +=(StringComponentInterpolatedStringHandler inteprolation)
    {
        if (OnNewLine is true)
        {
            OnNewLine = false;
            stringBuilder.Append("\n");
            for (var i = 0; i < Indentation; i++)
            {
                stringBuilder.Append(indentUnit);
            }
        }

        foreach (var part in inteprolation.Parts)
        {
            switch (part)
            {
                case InterpolatedStringPart.Literal(var str):
                    stringBuilder.Append(str);
                    break;
                case InterpolatedStringPart.InterpolatedString(var str):
                    stringBuilder.Append(
                        string.Join($"\n{new string(' ', Indentation * 2)}", str.Split('\n'))
                    );
                    break;

                case InterpolatedStringPart.StringComponent(var component):
                    component.RenderText(ref this);
                    break;
            }
        }
        OnNewLine = true;
    }

    public ActionDisposableStruct Indent(int? amount = null)
    {
        if (Layers.Count is 0)
            throw new ArgumentException("StringBuilderPlus is dead!!");

        var newIdentation = amount is { } notnull ? notnull : Indentation + 1;
        var layer = new Layer() { Identation = newIdentation };
        Layers.Push(layer);

        var layerIdentity = layer.Identity;
        var _Layers = Layers;
        return new ActionDisposableStruct(() =>
        {
            if (!object.ReferenceEquals(_Layers.Peek().Identity, layerIdentity))
                throw new InvalidOperationException(
                    "Popping another Layer from StringBuilderPlus then expected"
                );

            var layer = _Layers.Pop();
        });
    }

    public void Dispose()
    {
        var layer = Layers.Count switch
        {
            < 0 => throw new InvalidOperationException("Huh?"),
            0 => throw new InvalidOperationException("Drawables stack too empty"),
            1 => Layers.Pop(),
            > 1 => throw new InvalidOperationException("Drawables stack too full"),
        };
    }
}
