namespace AlternativeTextures.Framework.Models;

public class TokenModel
{
    // public required string Id { get; set; }
    public int Variation { get; set; }
    public required AlternativeTextureModel AlternativeTexture { get; set; }
}
