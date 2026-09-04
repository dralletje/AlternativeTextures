using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using ConsoleLog.Strings;

namespace ConsoleLog;

static class Util
{
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
            var textBuilder = new TextBuilder(_builder);
            textBuilder += new InspectComponent(value);
            // _builder.Append(PrettyPrint.Inspect(value));
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

    public static bool ContainsNewline(string text, int maxScanLength = 256)
    {
        return text.AsSpan(0, Math.Min(text.Length, maxScanLength)).IndexOfAny('\n', '\r') >= 0;
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
            var renderAsMultiline = ContainsNewline(s, 256) || s.Length > 256;
            if (renderAsMultiline)
            {
                return $""""
                    {"\"\"\"".Bold()}
                    {s}
                    {"\"\"\"".Bold()}
                    """".Green();
            }
            else
            {
                return $"\"{s}\"".Green();
            }
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
        else if (value is Exception exception)
        {
            return TextBuilder.Render(new ErrorPrinter(exception, maxdepth, depth));
        }
        else if (value is IFormattable && maxdepth == depth)
        {
            return $"{value.GetType().TechnicolorFullName()} {Inspect(value.ToString())}";
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
            return Indented.Create(
                $"""
                {type.TechnicolorFullName()}[
                    {list.Select(x => Inspect(x, maxdepth: maxdepth, depth: depth + 1))}
                ]
                """
            );
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
                $"{pv.Name.Magenta()}{":".BrightBlack()} {Inspect(pv.Value, maxdepth: maxdepth, depth: depth + 1)}"
            );
            var fieldStrings = fieldValues.Select(pv =>
                $"{pv.Name.Yellow()}{":".BrightBlack()} {Inspect(pv.Value, maxdepth: maxdepth, depth: depth + 1)}"
            );

            IEnumerable<string> extra = value switch
            {
                IFormattable formattable => [Inspect(formattable.ToString()), ""],
                _ => [],
            };

            return Indented.Create(
                $$"""
                {{typePrefix}}{{"{".BrightBlack()}}
                    {{[.. extra, .. fieldStrings, .. propStrings]}}
                {{"}".BrightBlack()}}
                """
            );
        }
    }
}

public record InspectComponent(object? value, int maxdepth = 3, int depth = 0) : ITextComponent
{
    public void RenderText(ref TextBuilder Text)
    {
        // Handle null case
        if (value is null)
        {
            Text += "null".Green();
        }
        else if (value is string s)
        {
            var renderAsMultiline = PrettyPrint.ContainsNewline(s, 256) || s.Length > 256;
            if (renderAsMultiline)
            {
                // Text += $""""
                //     {"\"\"\"".Bold()}
                //     {s}
                //     {"\"\"\"".Bold()}
                //     """".Green();
                Text += "\"\"\"".Green().Bold();
                Text += $"{s}".Green().Bold();
                Text += "\"\"\"".Green().Bold();
            }
            else
            {
                Text += $"\"{s}\"".Green();
            }
        }
        else if (value is bool b)
        {
            Text += b ? "true".Cyan() : "false".Cyan();
        }
        else if (value is char c)
        {
            Text += $"'{c}'".Green();
        }
        else if (value is decimal or float or double)
        {
            Text += value.ToString()!.Cyan();
        }
        else if (value.GetType().IsPrimitive)
        {
            Text += value.ToString()!.Cyan();
        }
        else if (value is Enum)
        {
            Text += value.ToString()!.Cyan();
        }
        else if (value is Exception exception)
        {
            Text += new ErrorPrinter(exception, maxdepth, depth);
        }
        else if (value is IFormattable && maxdepth == depth)
        {
            Text +=
                $"{value.GetType().TechnicolorFullName()} {new InspectComponent(value.ToString())}";
        }
        else if (value is ICollection nongenericList)
        {
            var type = nongenericList.GetType();
            var list = nongenericList.Cast<object>();

            if (!list.Any())
            {
                Text += $"{type.TechnicolorFullName()}{"[]".BrightBlack()}";
            }
            else if (depth >= maxdepth)
            {
                /// Hahahaha "1 items" (Ask LLM later idc)
                Text +=
                    $"{type.TechnicolorFullName()}{"[".BrightBlack()} {$"{list.Count()} items...".BrightBlack()} {"]".BrightBlack()}";
            }
            else
            {
                // Text += Indented.Create(
                //     $"""
                //     {type.TechnicolorFullName()}[
                //         {list.Select(x => Inspect(x, maxdepth: maxdepth, depth: depth + 1))}
                //     ]
                //     """
                // );
                Text += $"{type.TechnicolorFullName()}[";
                using (Text.Indent())
                {
                    foreach (var item in list)
                    {
                        Text += new InspectComponent(item, maxdepth: maxdepth, depth: depth + 1);
                    }
                }
                Text += $"]";
            }
        }
        else
        {
            /// Get the type of the object
            var type = value.GetType();
            var typePrefix = type.IsAnonymousType() ? "" : $"{type.TechnicolorFullName()} ";

            if (depth >= maxdepth)
            {
                Text += $"{typePrefix}{"{ ... }".BrightBlack()}";
                return;
            }

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
            {
                Text += $"{typePrefix}{"{}".BrightBlack()}";
                return;
            }

            Text += $"{typePrefix}{"{".BrightBlack()}";
            using (Text.Indent())
            {
                InspectComponent? extra = value switch
                {
                    IFormattable formattable => new InspectComponent(formattable.ToString()),
                    _ => null,
                };

                if (extra is not null)
                {
                    Text += extra;
                    Text += "";
                }

                foreach (var prop in propValues)
                {
                    Text +=
                        $"{prop.Name.Magenta()}{":".BrightBlack()} {new InspectComponent(prop.Value, maxdepth: maxdepth, depth: depth + 1)}";
                }

                foreach (var field in fieldValues)
                {
                    Text +=
                        $"{field.Name.Magenta()}{":".BrightBlack()} {new InspectComponent(field.Value, maxdepth: maxdepth, depth: depth + 1)}";
                }
            }
            Text += "}".BrightBlack();
        }
    }
}
