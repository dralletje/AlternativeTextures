using System.Runtime.CompilerServices;
using System.Text;

namespace ConsoleLog;

public interface TerminalColor
{
    string StartString { get; }
    string EndString { get; }

    // public struct NORMAL : TerminalColor
    // {
    //     public readonly string StartString => Colors.NORMAL;
    //     public readonly string ColorEnd => Colors.NORMAL;
    // }

    public struct RED : TerminalColor
    {
        public readonly string StartString => Colors.RED;
        public readonly string EndString => Colors.NORMAL;
    }

    public struct GREEN : TerminalColor
    {
        public readonly string StartString => Colors.GREEN;
        public readonly string EndString => Colors.NORMAL;
    }

    public struct YELLOW : TerminalColor
    {
        public readonly string StartString => Colors.YELLOW;
        public readonly string EndString => Colors.NORMAL;
    }

    public struct BLUE : TerminalColor
    {
        public readonly string StartString => Colors.BLUE;
        public readonly string EndString => Colors.NORMAL;
    }

    public struct MAGENTA : TerminalColor
    {
        public readonly string StartString => Colors.MAGENTA;
        public readonly string EndString => Colors.NORMAL;
    }

    public struct CYAN : TerminalColor
    {
        public readonly string StartString => Colors.CYAN;
        public readonly string EndString => Colors.NORMAL;
    }

    public struct GREY : TerminalColor
    {
        public readonly string StartString => Colors.GREY;
        public readonly string EndString => Colors.NORMAL;
    }

    public struct BOLD : TerminalColor
    {
        public readonly string StartString => Colors.BOLD;
        public readonly string EndString => Colors.NOBOLD;
    }

    public struct UNDERLINE : TerminalColor
    {
        public readonly string StartString => Colors.UNDERLINE;
        public readonly string EndString => Colors.NORMAL;
    }

    public struct REVERSE : TerminalColor
    {
        public readonly string StartString => Colors.REVERSE;
        public readonly string EndString => Colors.NORMAL;
    }

    public struct BRIGHT_BLACK : TerminalColor
    {
        public readonly string StartString => Colors.BRIGHT_BLACK;
        public readonly string EndString => Colors.NORMAL;
    }
}

// Must be a struct to avoid allocations and allow default(T) usage

/// <summary>
///  TODO Maybe just look at the nodejs colors?
/// </summary>
static class Colors
{
    public static string NORMAL = Console.IsOutputRedirected ? "" : "\x1b[39m";
    public static string RED = Console.IsOutputRedirected ? "" : "\x1b[31m";
    public static string GREEN = Console.IsOutputRedirected ? "" : "\x1b[32m";
    public static string YELLOW = Console.IsOutputRedirected ? "" : "\x1b[33m";
    public static string BLUE = Console.IsOutputRedirected ? "" : "\x1b[34m";
    public static string MAGENTA = Console.IsOutputRedirected ? "" : "\x1b[35m";
    public static string CYAN = Console.IsOutputRedirected ? "" : "\x1b[36m";
    public static string GREY = Console.IsOutputRedirected ? "" : "\x1b[37m";
    public static string BOLD = Console.IsOutputRedirected ? "" : "\x1b[1m";
    public static string NOBOLD = Console.IsOutputRedirected ? "" : "\x1b[22m";
    public static string UNDERLINE = Console.IsOutputRedirected ? "" : "\x1b[4m";
    public static string NOUNDERLINE = Console.IsOutputRedirected ? "" : "\x1b[24m";
    public static string REVERSE = Console.IsOutputRedirected ? "" : "\x1b[7m";
    public static string NOREVERSE = Console.IsOutputRedirected ? "" : "\x1b[27m";
    public static string BRIGHT_BLACK = Console.IsOutputRedirected ? "" : "\x1b[90m";

    ///
    public static string Red(ColorInterpolator<TerminalColor.RED> text) => text.ToString();

    public static string Green(ColorInterpolator<TerminalColor.GREEN> text) => text.ToString();

    public static string Yellow(ColorInterpolator<TerminalColor.YELLOW> text) => text.ToString();

    public static string Blue(ColorInterpolator<TerminalColor.BLUE> text) => text.ToString();

    public static string Magenta(ColorInterpolator<TerminalColor.MAGENTA> text) => text.ToString();

    public static string Cyan(ColorInterpolator<TerminalColor.CYAN> text) => text.ToString();

    public static string Grey(ColorInterpolator<TerminalColor.GREY> text) => text.ToString();

    public static string Bold(ColorInterpolator<TerminalColor.BOLD> text) => text.ToString();

    public static string Underline(ColorInterpolator<TerminalColor.UNDERLINE> text) =>
        text.ToString();

    public static string Reverse(ColorInterpolator<TerminalColor.REVERSE> text) => text.ToString();

    public static string BrightBlack(ColorInterpolator<TerminalColor.BRIGHT_BLACK> text) =>
        text.ToString();
}

public static class StringColorExtensions
{
    extension(string text)
    {
        public string Red() => $"{Colors.RED}{text}{Colors.NORMAL}";

        public string Green() => $"{Colors.GREEN}{text}{Colors.NORMAL}";

        public string Yellow() => $"{Colors.YELLOW}{text}{Colors.NORMAL}";

        public string Blue() => $"{Colors.BLUE}{text}{Colors.NORMAL}";

        public string Magenta() => $"{Colors.MAGENTA}{text}{Colors.NORMAL}";

        public string Cyan() => $"{Colors.CYAN}{text}{Colors.NORMAL}";

        public string Grey() => $"{Colors.GREY}{text}{Colors.NORMAL}";

        public string Bold() => $"{Colors.BOLD}{text}{Colors.NOBOLD}";

        public string Underline() => $"{Colors.UNDERLINE}{text}{Colors.NOUNDERLINE}";

        public string Reverse() => $"{Colors.REVERSE}{text}{Colors.NOREVERSE}";

        public string BrightBlack() => $"{Colors.BRIGHT_BLACK}{text}{Colors.NORMAL}";
    }
}

[InterpolatedStringHandler]
public ref struct ColorInterpolator<Color>(int literalLength, int formattedCount)
    where Color : struct, TerminalColor
{
    string ColorStart = default(Color).StartString;
    string ColorEnd = default(Color).EndString;
    private readonly StringBuilder builder = new(literalLength);

    public void AppendLiteral(string s)
    {
        builder.Append(ColorStart);
        builder.Append(s);
    }

    public void AppendFormatted<T>(T value, string? format = null)
    {
        builder.Append($"{value}");
    }

    bool DidAppendEnd = false;

    public override string ToString()
    {
        if (DidAppendEnd is false)
        {
            builder.Append(ColorEnd);
        }
        return builder.ToString();
    }
}
