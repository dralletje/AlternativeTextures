namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
sealed class UnionAttribute : Attribute;

interface IUnion
{
    object? Value { get; }
}
