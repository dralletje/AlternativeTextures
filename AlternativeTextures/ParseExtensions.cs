namespace AlternativeTextures;

static class ParseExtensions
{
    public static int? asInt(this string str)
    {
        if (int.TryParse(str, out var result))
        {
            return result;
        }
        else
        {
            return null;
        }
    }
}
