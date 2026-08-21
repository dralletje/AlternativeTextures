using System;
using System.Collections.Generic;
using System.Linq;

namespace AlternativeTextures.Framework.External.GenericModConfigMenu;

public record DisabledTexture()
{
    public required string TextureId { get; set; }
    public List<int> DisabledVariations { get; set; } = [];
}

public class ModConfig
{
    public bool OutputTextureDataToLog { get; set; }
    public bool UseRandomTexturesWhenSpawningArtifactSpots { get; set; } = true;
    public bool UseRandomTexturesWhenPlacingFlooring { get; set; } = false;
    public bool UseRandomTexturesWhenPlacingFruitTree { get; set; } = true;
    public bool UseRandomTexturesWhenPlacingTree { get; set; } = true;
    public bool UseRandomTexturesWhenPlacingHoeDirt { get; set; } = true;
    public bool UseRandomTexturesWhenPlacingGrass { get; set; } = true;
    public bool UseRandomTexturesWhenPlacingFurniture { get; set; } = false;
    public bool UseRandomTexturesWhenPlacingObject { get; set; } = false;
    public bool UseRandomTexturesWhenPlacingFarmAnimal { get; set; } = true;
    public bool UseRandomTexturesWhenPlacingMonster { get; set; } = true;
    public bool UseRandomTexturesWhenPlacingBuilding { get; set; } = false;
    public List<DisabledTexture> DisabledTextures { get; set; } = [];

    [Obsolete("Use UniqueTextureIdentifier variant")]
    internal bool IsTextureVariationDisabled(string textureId, int variation)
    {
        return DisabledTextures.Any(t =>
            t.TextureId.Equals(textureId, StringComparison.OrdinalIgnoreCase)
            && t.DisabledVariations.Contains(variation)
        );
    }

    internal bool IsTextureVariationDisabled(TextureIdentifierWithoutSeason identifier)
    {
        return DisabledTextures.Any(t =>
            t.TextureId.Equals(identifier.LegacyId, StringComparison.OrdinalIgnoreCase)
            && t.DisabledVariations.Contains(identifier.Variation)
        );
    }

    internal void SetTextureStatus(string textureId, int variation, bool isEnabled)
    {
        if (isEnabled)
        {
            if (
                !DisabledTextures.Any(t =>
                    t.TextureId.Equals(textureId, StringComparison.OrdinalIgnoreCase)
                    && t.DisabledVariations.Contains(variation)
                )
            )
            {
                return;
            }

            DisabledTextures
                .First(t =>
                    t.TextureId.Equals(textureId, StringComparison.OrdinalIgnoreCase)
                    && t.DisabledVariations.Contains(variation)
                )
                .DisabledVariations.Remove(variation);
        }
        else
        {
            var model = DisabledTextures.FirstOrDefault(t =>
                t.TextureId.Equals(textureId, StringComparison.OrdinalIgnoreCase)
            );
            if (model is null)
            {
                model = new DisabledTexture() { TextureId = textureId };
                DisabledTextures.Add(model);
            }

            if (!model.DisabledVariations.Contains(variation))
            {
                model.DisabledVariations.Add(variation);
            }
        }

        return;
    }
}
