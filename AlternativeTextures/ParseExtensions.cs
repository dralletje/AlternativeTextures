namespace AlternativeTextures;

static class ParseExtensions
{
    public static int? asInt(this string str)
    {
        return int.TryParse(str, out var result) ? result : null;
    }
}
