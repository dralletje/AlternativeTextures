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
    private List<string> _textureNames = [];
    private HashSet<string> _textureIdsInsensitive = [with(StringComparer.OrdinalIgnoreCase)];
    private Dictionary<string, Texture2D> _tokenToTextures = [with(StringComparer.OrdinalIgnoreCase)];
    private Dictionary<string, TokenModel> _tokenToModel = [with(StringComparer.OrdinalIgnoreCase)];

    private string _variationRegexPattern = @"AlternativeTextures\/Textures\/.*(?<variation>\d+)$";

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
            texturesByIdentifier.Add(model.UniqueIdentifier, model);
            _alternativeTextures.Add(model);
            _textureIdsInsensitive.Add(model.GetId());
        }

        RegisterTokens(model);
    }

    public void RegisterTokens(AlternativeTextureModel textureModel)
    {
        // Register for Content Patcher
        var token = $"{AlternativeTextures.TEXTURE_TOKEN_HEADER}{textureModel.GetTokenId()}";
        _tokenToModel[token] = new TokenModel()
        {
            // Id = token,
            AlternativeTexture = textureModel,
        };

        _textureNames.Add(textureModel.GetTokenId());
        foreach (var variation in textureModel.Textures.Keys)
        {
            _textureNames.Add(textureModel.GetTokenId(variation));

            token = $"{AlternativeTextures.TEXTURE_TOKEN_HEADER}{textureModel.GetTokenId(variation)}";
            _tokenToModel[token] = new TokenModel()
            {
                // Id = token,
                Variation = variation,
                AlternativeTexture = textureModel,
            };
        }
    }

    public List<AlternativeTextureModel> GetAllTextures()
    {
        return _alternativeTextures;
    }

    public List<string> GetValidTextureNames()
    {
        return [.. _alternativeTextures.Select(t => t.GetId())];
    }

    public List<string> GetValidTextureNamesWithSeason()
    {
        return _textureNames;
    }

    public bool DoesObjectHaveAlternativeTexture(TextureQuery query)
    {
        return _alternativeTextures.Any(t => t.ForModel == query.ModelIdentifier && t.Season == query.Season);
    }

    [Obsolete("Don't even know.")]
    public bool DoesObjectHaveAlternativeTexture(string objectName, bool isItemId = false)
    {
        return _alternativeTextures.Any(t =>
            t.IsUsingItemId() == isItemId
            && String.Equals(t.GetNameWithSeason(), objectName, StringComparison.OrdinalIgnoreCase)
        );
    }

    [Obsolete("Don't even know.")]
    public bool DoesObjectHaveAlternativeTextureById(string objectId)
    {
        return _textureIdsInsensitive.Contains(objectId);
    }

    public AlternativeTextureModel? GetRandomTextureModel(string objectName)
    {
        var validTextures = _alternativeTextures
            .Where(t => string.Equals(t.GetNameWithSeason(), objectName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return validTextures[Game1.random.Next(validTextures.Count)];
    }

    [Obsolete("Use .GetTexture(identifier)")]
    public AlternativeTextureModel? GetSpecificTextureModel(string textureId)
    {
        return !DoesObjectHaveAlternativeTextureById(textureId)
            ? null
            : _alternativeTextures.First(t => string.Equals(t.GetId(), textureId, StringComparison.OrdinalIgnoreCase));
    }

    [Obsolete("String modelname")]
    public List<AlternativeTextureModel> GetAvailableTextureModels(string modelName, Season season)
    {
        var modelNameWithSeason = string.Concat(modelName, "_", season);

        if (!DoesObjectHaveAlternativeTexture(modelName) && !DoesObjectHaveAlternativeTexture(modelNameWithSeason))
        {
            return [];
        }

        var seasonalTextures = _alternativeTextures
            .Where(t =>
                t.IsUsingItemId() is false
                && string.Equals(t.GetNameWithSeason(), modelNameWithSeason, StringComparison.OrdinalIgnoreCase)
            )
            .ToList();
        seasonalTextures.AddRange(
            _alternativeTextures.Where(t =>
                t.IsUsingItemId() is false
                && !seasonalTextures.Any(s => s.GetId() == t.GetId())
                && string.Equals(t.GetNameWithSeason(), modelName, StringComparison.OrdinalIgnoreCase)
            )
        );
        return seasonalTextures;
    }

    public List<AlternativeTextureModel> GetTexturesForModel(ModelIdentifier modelIdentifier, Season season)
    {
        return _alternativeTextures
            // .Where(t =>
            //     t.IsUsingItemId() is false
            //     && string.Equals(t.GetNameWithSeason(), modelNameWithSeason, StringComparison.OrdinalIgnoreCase)
            // )
            .Where(t => t.ForModel == modelIdentifier && t.Season == season)
            .ToList();
    }

    public AlternativeTextureModel? GetTexture(UniqueTextureIdentifier identifier)
    {
        return texturesByIdentifier.GetValueOrDefault(identifier);
    }

    public int GetVariationFromToken(string token)
    {
        var regex = new Regex(_variationRegexPattern);
        foreach (Match match in regex.Matches(token))
        {
            if (Int32.TryParse(match.Groups["variation"].ToString(), out var variation))
            {
                // Alert on failure
                return variation;
            }
        }

        return 0;
    }

    public Texture2D? GetTextureByToken(string token)
    {
        return String.IsNullOrEmpty(token) || _tokenToTextures.ContainsKey(token) is false
            ? null
            : _tokenToTextures[token];
    }

    public TokenModel? GetModelByToken(string token)
    {
        return String.IsNullOrEmpty(token) || _tokenToModel.ContainsKey(token) is false ? null : _tokenToModel[token];
    }

    public void UpdateTokenCache(string token)
    {
        _tokenToTextures[token] = _helper.GameContent.Load<Texture2D>(token);
    }

    public void UpdateTexture(string token, Texture2D texture)
    {
        if (String.IsNullOrEmpty(token) || _tokenToModel.ContainsKey(token) is false)
        {
            return;
        }

        var replacementIndex = _alternativeTextures.IndexOf(_tokenToModel[token].AlternativeTexture);
        /// TODO Also replace it in the other maps
        _alternativeTextures[replacementIndex] = _alternativeTextures[replacementIndex] with
        {
            Texture = new DrawableTexture(texture),
        };
    }
}
