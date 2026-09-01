using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using AlternativeTextures.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley.Characters;

namespace AlternativeTextures.PatchDrawMod.Patches.Entities;

internal class HorsePatch(IModHelper modHelper) : PatchTemplate()
{
    private readonly Type _entity = typeof(Horse);

    internal void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(_entity, nameof(Horse.draw), [typeof(SpriteBatch)]),
            postfix: new HarmonyMethod(GetType(), nameof(DrawPostfix))
        );
        harmony.Patch(
            AccessTools.Method(_entity, nameof(Horse.draw), [typeof(SpriteBatch)]),
            transpiler: new HarmonyMethod(typeof(HorsePatch), nameof(AdjustForVariationTranspiler))
        );
    }

    private static IEnumerable<CodeInstruction> AdjustForVariationTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        try
        {
            var list = instructions.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                if (
                    list[i].opcode == OpCodes.Callvirt
                    && list[i].operand is not null
                    && list[i].operand.ToString().Contains("updatesourcerect", StringComparison.OrdinalIgnoreCase)
                )
                {
                    list.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_0));
                    list.Insert(
                        i + 2,
                        new CodeInstruction(
                            OpCodes.Call,
                            AccessTools.Method(typeof(HorsePatch), nameof(HandleVariations), [typeof(Horse)])
                        )
                    );
                }

                if (list[i].opcode == OpCodes.Ldc_I4_S && (sbyte)list[i].operand == 96)
                {
                    list.Insert(i, new CodeInstruction(OpCodes.Ldarg_0));
                    list[i + 1] = new CodeInstruction(
                        OpCodes.Call,
                        AccessTools.Method(typeof(HorsePatch), nameof(GetHeadTextureYOffset), [typeof(Horse)])
                    );
                }
            }

            return list;
        }
        catch (Exception e)
        {
            Monitor.Log($"There was an issue modifying the instructions for Horse.draw: {e}", LogLevel.Error);
            return instructions;
        }
    }

    private static int GetHeadTextureYOffset(Horse horse)
    {
        var yOffset = 96;
        if (!horse.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME))
        {
            return yOffset;
        }

        var textureModel = AlternativeTextures.textureManager.GetSpecificTextureModel(
            horse.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME]
        );
        if (textureModel is null)
        {
            return yOffset;
        }

        var textureVariation = Int32.Parse(horse.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION]);
        return
            textureVariation == -1
            || AlternativeTextures.modConfig.IsTextureVariationDisabled(textureModel.LegacyId, textureVariation)
            ? yOffset
            : yOffset + textureModel.GetTextureOffset(textureVariation);
    }

    private static void HandleVariations(Horse horse)
    {
        if (!horse.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME))
        {
            return;
        }

        var textureModel = AlternativeTextures.textureManager.GetSpecificTextureModel(
            horse.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME]
        );
        if (textureModel is null)
        {
            return;
        }

        var textureVariation = Int32.Parse(horse.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION]);
        if (
            textureVariation == -1
            || AlternativeTextures.modConfig.IsTextureVariationDisabled(textureModel.LegacyId, textureVariation)
        )
        {
            return;
        }

        var textureOffset = textureModel.GetTextureOffset(textureVariation);
        horse.Sprite.spriteTexture = textureModel.GetTexture(textureVariation);
        horse.Sprite.sourceRect.Y =
            textureOffset
            + (
                horse.Sprite.currentFrame
                * horse.Sprite.SpriteWidth
                / horse.Sprite.Texture.Width
                * horse.Sprite.SpriteHeight
            );
    }

    [HarmonyBefore(["Goldenrevolver.HorseOverhaul"])]
    private static void DrawPostfix(Horse __instance, SpriteBatch b)
    {
        if (__instance.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME))
        {
            var textureModel = AlternativeTextures.textureManager.GetSpecificTextureModel(
                __instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME]
            );
            if (textureModel is null)
            {
                return;
            }

            var textureVariation = Int32.Parse(__instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION]);
            if (
                textureVariation == -1
                || AlternativeTextures.modConfig.IsTextureVariationDisabled(textureModel.LegacyId, textureVariation)
            )
            {
                return;
            }

            __instance.Sprite.UpdateSourceRect();
        }
    }
}
