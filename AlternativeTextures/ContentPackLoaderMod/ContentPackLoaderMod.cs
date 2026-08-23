using System;
using AlternativeTextures.Framework.Managers;
using AlternativeTextures.MetaFramework;
using Incubator;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace AlternativeTextures.ContentPackLoaderMod;

public sealed class ContentPackException(string message) : Exception(message);

public sealed class ContentPackTextureException(string message) : Exception(message);

class ContentPackLoaderMod<TParent>(DralModContext<TParent, ContentPackLoaderMod<TParent>> context) : DralMod
    where TParent : HasMod<TextureManager>
{
    readonly IManifest ModManifest = context.ModManifest;
    readonly IModHelper Helper = context.Helper;

    public override IDisposable? Entry()
    {
        Helper.Events.GameLoop.GameLaunched += this.Load;

        return new ActionDisposable(() =>
        {
            Helper.Events.GameLoop.GameLaunched -= this.Load;
        });
    }

    public void Load(object? sender, GameLaunchedEventArgs e)
    {
        var textureManager = context.Parent.GetMod<TextureManager>();

        ContentPackLoaderAsync.Load(Helper, textureManager);
        // ContentPackLoaderSync.Load(Helper, textureManager);
    }
}
