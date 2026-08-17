namespace AlternativeTextures.Framework;

struct TextureIdentifier(string owner, string name, string variation)
{
  public readonly string Owner = owner;
  public readonly string Name = name;
  public readonly string Variation = variation;
}