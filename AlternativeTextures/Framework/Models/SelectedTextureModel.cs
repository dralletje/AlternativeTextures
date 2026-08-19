using System.Collections.Generic;

namespace AlternativeTextures.Framework.Models;

public class SelectedTextureModel
{
    public required string Owner { get; set; }
    public required string TextureName { get; set; }
    public List<int> Variations { get; set; } = [];
}
