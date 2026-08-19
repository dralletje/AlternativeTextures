using System;
using System.Diagnostics.CodeAnalysis;
using AlternativeTextures.Framework.Models;

namespace AlternativeTextures.Framework;

sealed record TextureIdentifier() : IEquatable<TextureIdentifier>
{
    public required string Owner { get; init; }
    public required string Name { get; init; }
    public required int Variation { get; init; }

    [SetsRequiredMembers]
    public TextureIdentifier(string owner, string name, int variation)
        : this()
    {
        Owner = owner;
        Name = name;
        Variation = variation;
    }

    [SetsRequiredMembers]
    public TextureIdentifier(string owner, string name, string variation)
        : this()
    {
        Owner = owner;
        Name = name;
        Variation = int.Parse(variation);
    }

    [SetsRequiredMembers]
    public TextureIdentifier(AlternativeTextureModel model, int variation)
        : this(model.Owner, model.GetId(), variation) { }

    [SetsRequiredMembers]
    public TextureIdentifier(AlternativeTextureModel model, string variation)
        : this(model.Owner, model.GetId(), variation) { }

    public bool IsDefault
    {
        get { return this.Variation == -1 || this.Owner == AlternativeTextures.DEFAULT_OWNER; }
    }

    public static TextureIdentifier Default = new(AlternativeTextures.DEFAULT_OWNER, "", -1);

    public bool Equals(TextureIdentifier? other)
    {
        return other is not null && this.Name == other.Name && this.Variation == other.Variation;
    }

    public override int GetHashCode()
    {
        return this.Name.GetHashCode() * 17 + this.Variation;
    }
}
