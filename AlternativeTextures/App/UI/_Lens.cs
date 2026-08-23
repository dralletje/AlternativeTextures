using Incubator;

namespace AlternativeTextures.App.UI;

public readonly struct VariantLens<TOuter, TInner, TVariant>(IState<TOuter?> container) : IState<TInner?>
    where TVariant : IVariant<TVariant, TOuter, TInner>, TOuter
{
    public TInner? Value
    {
        get => container.Value is TVariant value ? value.Extract() : default;
        set
        {
            if (value == null)
            {
                container.Value = default;
            }
            else
            {
                container.Value = TVariant.Pack(value);
            }
        }
    }
}

public interface IVariant<TSelf, TOuter, TInner>
    where TSelf : IVariant<TSelf, TOuter, TInner>, TOuter
{
    abstract TInner? Extract();
    static abstract TSelf Pack(TInner inner);

    public static IState<TInner?> Lens(IState<TOuter?> container) => new VariantLens<TOuter, TInner, TSelf>(container);
}
