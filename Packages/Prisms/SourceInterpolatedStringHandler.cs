using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Prisms;

[InterpolatedStringHandler]
public ref struct SourceInterpolatedStringHandler(int literalLength, int formattedCount)
{
    private StringBuilder stringBuilder = new();
    private int lastIndentation = 0;

    static int indentation(string str)
    {
        ReadOnlySpan<char> trimmed = str.AsSpan().TrimEnd(' ');
        bool isValid = !trimmed.IsEmpty && (trimmed[^1] == '\n' || trimmed[^1] == '\r');
        int spaceCount = isValid ? str.Length - trimmed.Length : 0;
        return spaceCount;
    }

    public void AppendLiteral(string str)
    {
        lastIndentation = indentation(str);
        stringBuilder.Append(str);
    }

    public void AppendFormatted(string value)
    {
        stringBuilder.Append(value);
    }

    public void AppendFormatted(IEnumerable<string> strings)
    {
        stringBuilder.Append(string.Join($"\n{new string(' ', lastIndentation)}", strings));
    }

    public void AppendFormatted(INamespaceSymbol symbol)
    {
        /// TODO Format based on where it is being used (e.g. global::...)
        stringBuilder.Append(symbol.ToString());
    }

    public void AppendFormatted(ITypeSymbol symbol)
    {
        stringBuilder.Append(symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    public override string ToString()
    {
        return stringBuilder.ToString();
    }
}
