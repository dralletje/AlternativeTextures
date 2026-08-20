using System.Collections.Generic;

namespace Incubator;

static class GenericExtensions
{
    extension<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
        where TValue : class
    {
        public TValue? GetValueOrNull(TKey key)
        {
            return dictionary.TryGetValue(key, out var value) ? value : null;
        }
    }
}
