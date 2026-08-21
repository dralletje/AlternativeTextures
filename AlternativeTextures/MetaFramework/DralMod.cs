using System;
using StardewModdingAPI;

namespace AlternativeTextures.MetaFramework;

public record DralModContext<TParent, TMe>
{
    public required IModHelper Helper { get; init; }

    public required IMonitor Monitor { get; init; }

    public required IManifest ModManifest { get; init; }

    public required Func<TParent> GetParent { private get; init; }

    public required Func<TMe> GetMe { private get; init; }

    ////////////////////////

    public TParent Parent => GetParent();

    public T Scoped<T>(Func<DralModContext<TMe, T>, T> factory)
    {
        T? self = default(T);
        self = factory(
            new()
            {
                Monitor = Monitor,
                Helper = Helper,
                ModManifest = ModManifest,
                GetMe = () => self ?? throw new ArgumentException("Can't get parent mod during initialisation"),
                GetParent = () => GetMe(),
            }
        );
        return self;
    }

    public DralModContext() { }

    public DralModContext(DralModContext<Object, TParent> parent, Func<DralModContext<TParent, TMe>, TMe> factory)
    {
        TMe? self = default(TMe);
        Monitor = parent.Monitor;
        Helper = parent.Helper;
        ModManifest = parent.ModManifest;
        GetMe = () => self ?? throw new ArgumentException("Can't get parent mod during initialisation");
        GetParent = () => parent.GetMe();
        self = factory(this);
    }
}

public abstract class DralMod
{
    public abstract IDisposable? Entry();
}

public interface HasMod<T>
{
    public T GetMod();
}

public static class ModExtensions
{
    public static T GetMod<T>(this HasMod<T> context) => context.GetMod();
}
