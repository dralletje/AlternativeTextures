using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AlternativeTextures.Framework.Models;
using ConsoleLog;
using Incubator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace AlternativeTextures.Framework.Managers;

internal class TextureManager(IMod mod)
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

    private IModHelper _helper = mod.Helper;

    private List<AlternativeTextureModel> _alternativeTextures = [];
    private HashSet<string> _textureIdsInsensitive = [with(StringComparer.OrdinalIgnoreCase)];
    private Dictionary<string, AlternativeTextureModel> tokenToModel = [with(StringComparer.OrdinalIgnoreCase)];

    static int? FindIndexOrNull<T>(List<T> haystack, System.Predicate<T> findNeedle)
    {
        var result = haystack.FindIndex(findNeedle);
        return result is -1 ? null : result;
    }

    public Dictionary<UniqueTextureIdentifier, AlternativeTextureModel> texturesByIdentifier = [];

    public void AddAlternativeTexture(AlternativeTextureModel model)
    {
        if (FindIndexOrNull(_alternativeTextures, t => t.UniqueIdentifier == model.UniqueIdentifier) is { } index)
        {
            _alternativeTextures[index] = model;
        }
        else
        {
            _alternativeTextures.Add(model);
        }

        texturesByIdentifier.Add(model.UniqueIdentifier, model);
        _textureIdsInsensitive.Add(model.LegacyId);

        var token = $"{AlternativeTextures.TEXTURE_TOKEN_HEADER}{model.GetTokenId()}";
        tokenToModel[token] = model;
    }

    public List<AlternativeTextureModel> GetAllTextures()
    {
        return _alternativeTextures;
    }

    [Obsolete("Use .GetTexture(identifier)")]
    public AlternativeTextureModel? GetSpecificTextureModel(string textureId)
    {
        return _alternativeTextures.FirstOrDefault(t =>
            string.Equals(t.LegacyId, textureId, StringComparison.OrdinalIgnoreCase)
        );
    }

    public List<AlternativeTextureModel> GetTexturesForModel(ModelIdentifier modelIdentifier, Season season)
    {
        return _alternativeTextures.Where(t => t.ForModel == modelIdentifier && t.Season == season).ToList();
    }

    public AlternativeTextureModel? GetTexture(UniqueTextureIdentifier identifier)
    {
        return texturesByIdentifier.GetValueOrNull(identifier);
    }

    public AlternativeTextureModel? GetModelByToken(string token)
    {
        return tokenToModel.GetValueOrNull(token);
    }
}
