using System;
using AlternativeTextures.Framework;
using HarmonyLib;
using Incubator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace AlternativeTextures.PatchDrawMod.Patches;

internal class FarmAnimalPatch(IModHelper modHelper) : PatchTemplate()
{
    private readonly Type _entity = typeof(FarmAnimal);

    internal void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(_entity, nameof(FarmAnimal.draw), [typeof(SpriteBatch)]),
            prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix))
        );
        harmony.Patch(
            AccessTools.Method(
                _entity,
                nameof(FarmAnimal.updateWhenCurrentLocation),
                [typeof(GameTime), typeof(GameLocation)]
            ),
            postfix: new HarmonyMethod(GetType(), nameof(UpdateWhenCurrentLocationPostfix))
        );
    }

    private static bool DrawPrefix(Character __instance, SpriteBatch b)
    {
        if (
            GetTextureForUse(
                __instance.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_NAME),
                __instance.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION)
            ) is
            { } modelTexture
        )
        {
            /// NOTE I don't think this is a problem, but who knows?
            /// (The fact that I now don't explicitly set loadedTexture to an empty string)
            // if (textureModel is null)
            // {
            //     __instance.Sprite.loadedTexture = String.Empty;
            //     return true;
            // }

            /// TODO Make this work with modelTexture.Texture.SourceRect, you know you want to
            __instance.Sprite.spriteTexture = modelTexture.Texture.Texture;
            __instance.Sprite.sourceRect.Y =
                __instance.Sprite.currentFrame
                * __instance.Sprite.SpriteWidth
                / __instance.Sprite.Texture.Width
                * __instance.Sprite.SpriteHeight;
        }

        return true;
    }

    private static void UpdateWhenCurrentLocationPostfix(FarmAnimal __instance, GameTime time, GameLocation location)
    {
        if (
            __instance.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) is not { } texturename
            || __instance.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION) is not { } variation
            || __instance.modData.GetValueOrNull(ModDataKeys.ALTERNATIVE_TEXTURE_OWNER) is not { } owner
        )
            return;

        var textureIdentifier = TextureIdentifierWithoutSeason.FromString(texturename, variation);
        var season = Game1.GetSeasonForLocation(__instance.currentLocation);
        var withSeason = textureIdentifier.WithSeason(season);

        if (!string.Equals(texturename, withSeason.LegacyId, StringComparison.OrdinalIgnoreCase))
        {
            __instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = withSeason.LegacyId;
        }
    }
}
