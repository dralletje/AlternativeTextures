namespace Incubator;

static class NetcodeExtensions
{
    extension<TKey, TValue, TField, TSerialDict, TSelf>(
        Netcode.NetDictionary<TKey, TValue, TField, TSerialDict, TSelf> dictionary
    )
        where TValue : class
        where TField : class, Netcode.INetObject<Netcode.INetSerializable>, new()
        where TSerialDict : System.Collections.Generic.IDictionary<TKey, TValue>, new()
        where TSelf : Netcode.NetDictionary<TKey, TValue, TField, TSerialDict, TSelf>
    {
        public TValue? GetValueOrNull(TKey key)
        {
            return dictionary.TryGetValue(key, out var value) ? value : null;
        }
    }
}
