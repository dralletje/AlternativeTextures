namespace Incubator;

public abstract record FocusElement
{
    // 3. Define the union variants as inner partial records.
    [Dral.Optics.Generate.Prism]
    public record InScrollbar2(int Value) : FocusElement
    {
        // public VerticalScrollbar.FocusElement Extract() => this.focusElement;

        // public static InScrollbar Pack(VerticalScrollbar.FocusElement inner) => new InScrollbar(inner);

        // public static IState<VerticalScrollbar.FocusElement> Lens(IState<FocusElement?> container) =>
        //     new VariantLens<FocusElement, VerticalScrollbar.FocusElement, InScrollbar>(container);
    }

    public record InGrid((int column, int row) Element) : FocusElement;
}

static class X
{
    static void x()
    {
        var l = FocusElement.InScrollbar2.Prism;
    }
}
