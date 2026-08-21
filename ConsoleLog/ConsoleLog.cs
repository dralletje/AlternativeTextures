using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;

namespace ConsoleLog;

/// <summary>
///  TODO Maybe just look at the nodejs colors?
/// </summary>
public static class Colors
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

    // extension(string text)
    public static string Red(this string text) => $"{RED}{text}{NORMAL}";

    public static string Green(this string text) => $"{GREEN}{text}{NORMAL}";

    public static string Yellow(this string text) => $"{YELLOW}{text}{NORMAL}";

    public static string Blue(this string text) => $"{BLUE}{text}{NORMAL}";

    public static string Magenta(this string text) => $"{MAGENTA}{text}{NORMAL}";

    public static string Cyan(this string text) => $"{CYAN}{text}{NORMAL}";

    public static string Grey(this string text) => $"{GREY}{text}{NORMAL}";

    public static string Bold(this string text) => $"{BOLD}{text}{NOBOLD}";

    public static string Underline(this string text) => $"{UNDERLINE}{text}{NOUNDERLINE}";

    public static string Reverse(this string text) => $"{REVERSE}{text}{NOREVERSE}";

    public static string BrightBlack(this string text) => $"{BRIGHT_BLACK}{text}{NORMAL}";
}

static class Util
{
    // extension(string text)
    public static string Indent(this string text, string prefix)
    {
        return string.Join("\n", text.Split('\n').Select(line => prefix + line));
    }

    // extension(Type type)
    public static string FullName(this Type type)
    {
        if (type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();
            var typeName =
                type.Name.Split('`')[0]
                + "<"
                + string.Join(", ", genericArgs.Select(t => t.Name))
                + ">";
            return typeName;
        }
        else
        {
            return type.Name;
        }
    }

    public static string TechnicolorFullName(this Type type)
    {
        if (type.IsAnonymousType())
        {
            return "Anonymous".BrightBlack();
        }
        if (type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();
            var genericPart = "<" + string.Join(", ", genericArgs.Select(t => t.Name)) + ">";
            var typeName = type.Name.Split('`')[0].Blue() + genericPart.BrightBlack();
            return typeName;
        }
        else
        {
            return type.Name.Blue();
        }
    }

    /// https://stackoverflow.com/questions/2483023/how-to-test-if-a-type-is-anonymous
    public static bool IsAnonymousType(this Type type)
    {
        return Attribute.IsDefined(
                type,
                typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute),
                false
            )
            && type.IsGenericType
            && type.Name.Contains("AnonymousType")
            && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
            && (type.Attributes & System.Reflection.TypeAttributes.NotPublic)
                == System.Reflection.TypeAttributes.NotPublic;
    }
}

[InterpolatedStringHandler]
public ref struct InspectString(int literalLength, int formattedCount)
{
    private readonly StringBuilder _builder = new StringBuilder(literalLength);

    public void AppendLiteral(string s)
    {
        _builder.Append(s);
    }

    public void AppendFormatted<T>(T value, string? format = null)
    {
        if (format == "raw")
        {
            _builder.Append(value);
        }
        else
        {
            _builder.Append(PrettyPrint.Inspect(value));
        }
    }

    public override string ToString() => _builder.ToString();
}

public static class PrettyPrint
{
    extension(Console)
    {
        public static void Log(InspectString message)
        {
            Console.WriteLine(message.ToString());
        }
    }

    /// <summary>
    /// I'd love it to be an extension method on Console, but then it complains about
    /// null reference something something
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    // public static void Log(InspectString message)
    // {
    //     Console.WriteLine(message.ToString());
    // }

    public static void Log<T>(T message, params object?[] args)
    {
        var parameters = (object?[])[message, .. args];
        var as_string = string.Join(" ", parameters.Select(x => x is string s ? s : Inspect(x)));
        Console.WriteLine(as_string);
    }

    public static string InspectFormat(InspectString message)
    {
        return message.ToString();
    }

    public static string Inspect(object? value, int maxdepth = 3, int depth = 0)
    {
        // Handle null case
        if (value is null)
        {
            return "null".Green();
        }
        else if (value is string s)
        {
            return $"\"{s}\"".Green();
        }
        else if (value is bool b)
        {
            return b ? "true".Cyan() : "false".Cyan();
        }
        else if (value is char c)
        {
            return $"'{c}'".Green();
        }
        else if (value is decimal or float or double)
        {
            return value.ToString()!.Cyan();
        }
        else if (value.GetType().IsPrimitive)
        {
            return value.ToString()!.Cyan();
        }
        else if (value is Enum)
        {
            return value.ToString()!.Cyan();
        }
        else if (value is ICollection nongenericList)
        {
            var type = nongenericList.GetType();
            var list = nongenericList.Cast<object>();

            if (!list.Any())
                return $"{type.TechnicolorFullName()}{"[]".BrightBlack()}";
            /// Hahahaha 1 items (Ask LLM later idc)
            if (depth >= maxdepth)
                return $"{type.TechnicolorFullName()}{"[".BrightBlack()} {$"{list.Count()} items...".BrightBlack()} {"]".BrightBlack()}";
            return $"""
                {type.TechnicolorFullName()}[
                {string.Join(
                        "\n",
                        list.Select(x => Inspect(x, maxdepth: maxdepth, depth: depth + 1))
                    ).Indent("  ")}
                ]
                """;
        }
        else
        {
            /// Get the type of the object
            var type = value.GetType();
            var typePrefix = type.IsAnonymousType() ? "" : $"{type.TechnicolorFullName()} ";

            if (depth >= maxdepth)
                return $"{typePrefix}{"{ ... }".BrightBlack()}";

            /// Get the public properties of the object
            var properties = value
                .GetType()
                // .GetProperties(System.Reflection.BindingFlags.Instance)
                .GetProperties()
                .Where(p => p.GetIndexParameters().Length == 0)
                .Where(p => p.GetMethod is { } getMethod && !getMethod.IsStatic)
                .Where(p => p.GetMethod is { } getMethod && getMethod.IsPublic)
                .ToList();

            /// Get the name and value of each property
            var propValues = properties
                .Select(p =>
                {
                    try
                    {
                        return new { Name = p.Name, Value = p.GetValue(value) };
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                })
                .Where(x => x is not null)
                .Select(x => x!)
                .ToList();

            var fields = value
                .GetType()
                // .GetProperties(System.Reflection.BindingFlags.Instance)
                .GetFields()
                .Where(p => p.IsPublic && !p.IsStatic)
                .ToList();

            var fieldValues = fields
                .Select(p =>
                {
                    try
                    {
                        return new { Name = p.Name, Value = p.GetValue(value) };
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                })
                .Where(x => x is not null)
                .Select(x => x!)
                .ToList();

            if (properties.Count == 0 && fieldValues.Count == 0)
                return $"{typePrefix}{"{}".BrightBlack()}";

            var propStrings = propValues.Select(pv =>
                $"{pv.Name.Magenta()}: {Inspect(pv.Value, maxdepth: maxdepth, depth: depth + 1)}"
            );
            var fieldStrings = fieldValues.Select(pv =>
                $"{pv.Name.Yellow()}: {Inspect(pv.Value, maxdepth: maxdepth, depth: depth + 1)}"
            );
            var props = string.Join("\n", [.. fieldStrings, .. propStrings]);
            return $$"""
                {{typePrefix}}{{"{".BrightBlack()}}
                {{props.Indent("  ")}}
                {{"}".BrightBlack()}}
                """;
        }
    }
}
