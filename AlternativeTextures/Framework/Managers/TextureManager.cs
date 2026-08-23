using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Incubator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace AlternativeTextures.Framework.Managers;

internal class ModelsByIdentifier : KeyedCollection<UniqueTextureIdentifier, AlternativeTextureModel>
{
    protected override UniqueTextureIdentifier GetKeyForItem(AlternativeTextureModel item) => item.UniqueIdentifier;
}

internal class TextureManager(IModHelper helper)
{
    public readonly Texture2D ErrorTexture = Function.Run<Texture2D>(() =>
    {
        var ErrorTexture = new Texture2D(Game1.graphics.GraphicsDevice, 16, 16);
        Color[] data = new Color[16 * 16];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = Color.White;
        }
        ErrorTexture.SetData(Enumerable.Repeat(Color.White, 16 * 16).ToArray());
        return ErrorTexture;
    });

    private ModelsByIdentifier textures = [];
    private Dictionary<string, AlternativeTextureModel> legacyIdToModel = [with(StringComparer.OrdinalIgnoreCase)];
    private Dictionary<string, AlternativeTextureModel> texturePathToModel = [with(StringComparer.OrdinalIgnoreCase)];

    public void AddAlternativeTexture(AlternativeTextureModel model)
    {
        textures.Add(model);
        legacyIdToModel[model.LegacyId] = model;
        texturePathToModel[model.TexturePath] = model;
    }

    public IEnumerable<AlternativeTextureModel> GetAllTextures()
    {
        return textures;
    }

    [Obsolete("Use .GetTexture(identifier)")]
    public AlternativeTextureModel? GetSpecificTextureModel(string textureId)
    {
        return legacyIdToModel.GetValueOrNull(textureId);
    }

    public List<AlternativeTextureModel> GetTexturesForModel(ModelIdentifier modelIdentifier, Season season)
    {
        return textures.Where(t => t.ForModel == modelIdentifier && t.Season == season).ToList();
    }

    public AlternativeTextureModel? GetTexture(UniqueTextureIdentifier identifier)
    {
        return textures.TryGetValue(identifier, out var model) ? model : null;
    }

    public AlternativeTextureModel? GetTextureForPath(string path)
    {
        return texturePathToModel.GetValueOrNull(path);
    }
}
