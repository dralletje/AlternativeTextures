using System;

namespace AlternativeTextures.Framework.Utilities.Extensions;

public static class StringExtension
{
    public static string ReplaceLastInstance(this string source, string target, string replacement)
    {
        var index = source.LastIndexOf(target, StringComparison.OrdinalIgnoreCase);

        return index == -1 ? source : source.Remove(index, target.Length).Insert(index, replacement);
    }
}
