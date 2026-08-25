using System.Runtime.CompilerServices;
using System.Text;

namespace ConsoleLog;

[InterpolatedStringHandler]
ref struct IndentedInterpolatedStringHandler(int literalLength, int formattedCount)
{
    private StringBuilder stringBuilder = new();
    private string currentIdentation = "";

    static string indentation(string str)
    {
        ReadOnlySpan<char> trimmed = str.AsSpan().TrimEnd(' ');
        bool isValid = !trimmed.IsEmpty && (trimmed[^1] == '\n' || trimmed[^1] == '\r');
        int spaceCount = isValid ? str.Length - trimmed.Length : 0;
        return new string(' ', spaceCount);
    }

    public void AppendLiteral(string str)
    {
        currentIdentation = indentation(str);
        stringBuilder.Append(str);
    }

    public void AppendFormatted(string value)
    {
        var withIndentation = string.Join($"\n{currentIdentation}", value.Split('\n'));
        stringBuilder.Append(withIndentation);
    }

    public void AppendFormatted(IEnumerable<string> strings)
    {
        foreach (var (index, str) in strings.Select((x, index) => (index: index, value: x)))
        {
            if (index is not 0)
            {
                stringBuilder.Append("\n");
                stringBuilder.Append(currentIdentation);
            }
            AppendFormatted(str);
        }
    }

    public override string ToString()
    {
        return stringBuilder.ToString();
    }
}

static class Indented
{
    public static string Create(IndentedInterpolatedStringHandler builder) => builder.ToString();

    extension(string text)
    {
        public string Indent(string prefix)
        {
            return string.Join("\n", text.Split('\n').Select(line => prefix + line));
        }
    }
}
