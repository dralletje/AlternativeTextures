using System;
using AlternativeTextures.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.GiantCrops;
using StardewValley.TerrainFeatures;

namespace AlternativeTextures.PatchDrawMod.Patches.StandardObjects;

internal class GiantCropPatch(IModHelper modHelper) : PatchTemplate()
{
    private readonly Type _object = typeof(GiantCrop);

    internal void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(_object, nameof(GiantCrop.draw), [typeof(SpriteBatch)]),
            prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix))
        );

        if (PatchTemplate.IsDGAUsed())
        {
            try
            {
                if (
                    Type.GetType("DynamicGameAssets.Game.CustomGiantCrop, DynamicGameAssets") is Type dgaGiantCropType
                    && dgaGiantCropType != null
                )
                {
                    harmony.Patch(
                        AccessTools.Method(
                            dgaGiantCropType,
                            nameof(GiantCrop.draw),
                            [typeof(SpriteBatch), typeof(Vector2)]
                        ),
                        prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix))
                    );
                }
            }
            catch (Exception ex)
            {
                Monitor.Log(
                    $"Failed to patch Dynamic Game Assets in {this.GetType().Name}: AT may not be able to override certain DGA object types!",
                    LogLevel.Warn
                );
                Monitor.Log($"Patch for DGA failed in {this.GetType().Name}: {ex}", LogLevel.Trace);
            }
        }
    }

    [HarmonyBefore(["spacechase0.JsonAssets", "spacechase0.MoreGiantCrops"])]
    private static bool DrawPrefix(GiantCrop __instance, float ___shakeTimer, SpriteBatch spriteBatch)
    {
        if (__instance.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME))
        {
            var tileLocation = __instance.Tile;

            var textureModel = AlternativeTextures.textureManager.GetSpecificTextureModel(
                __instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME]
            );
            if (textureModel is null)
            {
                return true;
            }

            var textureVariation = Int32.Parse(__instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION]);
            if (
                textureVariation == -1
                || AlternativeTextures.modConfig.IsTextureVariationDisabled(textureModel.GetId(), textureVariation)
            )
            {
                return true;
            }

            var textureOffset = textureModel.GetTextureOffset(textureVariation);
            spriteBatch.Draw(
                textureModel.GetTexture(textureVariation),
                Game1.GlobalToLocal(
                    Game1.viewport,
                    (tileLocation * 64f)
                        - new Vector2(
                            (___shakeTimer > 0f) ? ((float)Math.Sin(Math.PI * 2.0 / (double)___shakeTimer) * 2f) : 0f,
                            64f
                        )
                ),
                new Rectangle(0, textureOffset, 48, 63),
                Color.White,
                0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                (tileLocation.Y + 2f) * 64f / 10000f
            );

            return false;
        }

        return true;
    }

    internal static bool TryGetGiantCropName(GiantCrop giantCrop, out string instanceName)
    {
        instanceName = String.Empty;
        if (giantCrop.GetData() is not GiantCropData giantCropData)
            return false;
        instanceName = ItemRegistry.GetData(giantCropData.FromItemId)?.InternalName ?? String.Empty;
        instanceName = $"{TextureType.GiantCrop}_{instanceName}";
        return true;
    }
}
