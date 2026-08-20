using System;
using AlternativeTextures.Framework.Models;
using AlternativeTextures.Framework.Utilities;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace AlternativeTextures.Framework.Patches.Entities;

internal class MonsterPatch(IMonitor modMonitor, IModHelper modHelper) : PatchTemplate()
{
    private readonly Type _entity = typeof(Monster);

    internal void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(_entity, nameof(Monster.draw), [typeof(SpriteBatch)]),
            prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix))
        );
        harmony.Patch(
            AccessTools.Method(_entity, nameof(Monster.update), [typeof(GameTime), typeof(GameLocation)]),
            postfix: new HarmonyMethod(GetType(), nameof(UpdatePostfix))
        );
        harmony.Patch(
            AccessTools.Method(_entity, nameof(Monster.reloadSprite), null),
            postfix: new HarmonyMethod(GetType(), nameof(ReloadSpritePostfix))
        );
    }

    private static void SetTexture(Monster monster, AlternativeTextureModel textureModel)
    {
        monster.Sprite.spriteTexture = textureModel.Texture.Texture;
        monster.Sprite.sourceRect.Y =
            monster.Sprite.currentFrame
            * monster.Sprite.SpriteWidth
            / monster.Sprite.Texture.Width
            * monster.Sprite.SpriteHeight;
    }

    private static bool DrawPrefix(Monster __instance, SpriteBatch b)
    {
        if (
            GetTextureForUse(
                __instance.modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_NAME),
                __instance.modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION)
            ) is
            { } textureModel
        )
        {
            SetTexture(__instance, textureModel);
        }

        return true;
    }

    private static void UpdatePostfix(Monster __instance, GameTime time, GameLocation location)
    {
        if (!__instance.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME))
        {
            return;
        }

        if (
            GetTextureForUse(
                __instance.modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_NAME),
                __instance.modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION)
            ) is
            { } textureModel
        )
        {
            if (__instance.Sprite.textureName.Value != textureModel.Texture.Texture.Name)
            {
                SetTexture(__instance, textureModel);
            }
        }
    }

    private static void ReloadSpritePostfix(Monster __instance)
    {
        if (
            GetTextureForUse(
                __instance.modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_NAME),
                __instance.modData.GetValueOrDefault(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION)
            ) is
            { } textureModel
        )
        {
            SetTexture(__instance, textureModel);
        }
    }
}
