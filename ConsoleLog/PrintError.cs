using System;
using System.Diagnostics;
using System.Text;
using ConsoleLog.Strings;

namespace ConsoleLog;

public class ErrorPrinter(Exception exception, int maxdepth, int depth) : ITextComponent
{
    static List<(string Name, object Value)> GetFields(object value)
    {
        var fields = value
            .GetType()
            // .GetProperties(System.Reflection.BindingFlags.Instance)
            .GetFields()
            .Where(p => p.IsPublic && !p.IsStatic)
            .ToList();

        return fields
            .Select(p =>
            {
                try
                {
                    return (Name: p.Name, Value: p.GetValue(value));
                }
                catch (Exception error)
                {
                    return (Name: p.Name, Value: error);
                }
            })
            .ToList();
    }

    public void RenderText(ref TextBuilder Text)
    {
        // sb.AppendLine($"{$"[{currentEx.GetType().Name}]".Red()} {currentEx.Message}");

        var st = new StackTrace(exception, true);
        var frames = st.GetFrames() ?? [];

        // foreach (var frame in frames)
        // {
        //     var method = frame.GetMethod();
        //     var type = method?.DeclaringType;

        //     if (
        //         type != null
        //         && (type.FullName.StartsWith("System.") || type.FullName.StartsWith("Microsoft."))
        //     )
        //         continue;

        //     var methodName = type == null ? method?.Name : $"{type.FullName}.{method.Name}";
        //     var file = frame.GetFileName();
        //     var line = frame.GetFileLineNumber();

        //     var fileInfo =
        //         file != null ? Colors.BrightBlack($" in {file.Blue()}:{line}") : string.Empty;
        //     sb.AppendLine(Colors.BrightBlack($"{indent}  at {methodName}{fileInfo}"));
        // }

        Text += $"{exception.GetType().Name.Blue()} {"{".BrightBlack()}";

        using (Text.Indent())
        {
            Text += exception.Message.Red().Bold();

            using (Text.Indent())
            {
                foreach (
                    var line in from frame in frames
                    let method = frame.GetMethod()
                    let type = method?.DeclaringType
                    where type is not null
                    where
                        !(
                            type.FullName.StartsWith("System.")
                            || type.FullName.StartsWith("Microsoft.")
                        )
                    let methodName = (
                        type is null ? method?.Name : $"{type.FullName}.{method.Name}"
                    )
                    let file = frame.GetFileName()
                    let line = frame.GetFileLineNumber()
                    let fileInfo = (
                        file != null
                            ? Colors.BrightBlack($" in {file.Blue()}:{line}")
                            : string.Empty
                    )
                    select Colors.BrightBlack($"at {methodName}{fileInfo}")
                )
                {
                    Text += line;
                }
            }

            foreach (var field in GetFields(exception))
            {
                Text += "";
                Text +=
                    $"{field.Name.Yellow()}: {new InspectComponent(field.Value, maxdepth: maxdepth, depth: depth + 1)}";
            }
            if (exception.InnerException is { } InnerException)
            {
                Text += "";
                Text +=
                    $"{"InnerException".Yellow()}: {new InspectComponent(InnerException, maxdepth: maxdepth, depth: depth + 1)}";
            }
        }
        Text += "}".BrightBlack();
        // currentEx = currentEx.InnerException;
        // if (currentEx != null)
        // {
        //     depth++;
        //     sb.AppendLine($"{indent}--- Inner Exception ---".Yellow());
        // }
    }
}
