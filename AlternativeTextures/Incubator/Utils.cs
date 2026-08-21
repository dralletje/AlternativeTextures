using System;

namespace Incubator;

public static class EnumUtil
{
    extension(Enum _)
    {
        public static TEnum? ParseOrNull<TEnum>(string value, bool ignoreCase = false)
            where TEnum : struct, Enum
        {
            return Enum.TryParse(value, ignoreCase, out TEnum result) ? result : null;
        }
    }
}

static class Function
{
    public static T Tap<T>(Func<T> function)
    {
        return function();
    }

    public static T Run<T>(Func<T> function)
    {
        return function();
    }
}
