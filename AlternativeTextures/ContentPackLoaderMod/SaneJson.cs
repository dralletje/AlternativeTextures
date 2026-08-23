namespace AlternativeTextures.ContentPackLoaderMod;

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public class StrictNullabilityContractResolver : DefaultContractResolver
{
    public static StrictNullabilityContractResolver Shared = new();

    private static readonly NullabilityInfoContext _nullabilityContext = new();

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);

        // 1. C# 11 `required` keyword
        bool isRequired = member.GetCustomAttribute<RequiredMemberAttribute>() != null;

        // 2. Non-nullable reference / value types
        bool isNonNullable = member switch
        {
            PropertyInfo pi => IsNonNullable(pi),
            FieldInfo fi => IsNonNullable(fi),
            _ => false,
        };

        if (isRequired)
        {
            // Must exist in JSON; enforce null constraint based on NRT
            property.Required = isNonNullable ? Required.Always : Required.AllowNull;
        }
        else if (isNonNullable)
        {
            // Optional in JSON, but cannot be explicitly passed as null
            property.Required = Required.DisallowNull;
        }

        return property;
    }

    private static bool IsNonNullable(PropertyInfo pi)
    {
        if (Nullable.GetUnderlyingType(pi.PropertyType) != null)
            return false;
        if (pi.PropertyType.IsValueType)
            return true;
        return _nullabilityContext.Create(pi).WriteState is NullabilityState.NotNull;
    }

    private static bool IsNonNullable(FieldInfo fi)
    {
        if (Nullable.GetUnderlyingType(fi.FieldType) != null)
            return false;
        if (fi.FieldType.IsValueType)
            return true;
        return _nullabilityContext.Create(fi).WriteState is NullabilityState.NotNull;
    }
}
