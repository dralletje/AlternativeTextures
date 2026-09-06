namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
sealed class ClosedAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
sealed class IsClosedTypeAttribute : Attribute
{
    public IsClosedTypeAttribute() { }
}
