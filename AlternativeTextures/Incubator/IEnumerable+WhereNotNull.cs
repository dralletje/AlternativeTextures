using System.Collections.Generic;
using System.Linq;

namespace Incubator;

public static class IEnumerable_WhereNotNull
{
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
        where T : class => source.Where(x => x is not null)!;

    /// TODO Not sure about this one... (I don't know if `.HasValue` works like that)
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
        where T : struct => source.Where(x => x.HasValue).Select(x => x!.Value);
}
