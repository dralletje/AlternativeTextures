using Microsoft.CodeAnalysis;

public static class AccessibilityUtility
{
    public static string GetEffectiveAccessibilityKeyword(ISymbol symbol)
    {
        var minAccessibility = symbol.DeclaredAccessibility;

        // Traverse up containing types to account for nesting visibility limits
        var current = symbol.ContainingType;
        while (current is not null)
        {
            if (IsStricter(current.DeclaredAccessibility, minAccessibility))
            {
                minAccessibility = current.DeclaredAccessibility;
            }
            current = current.ContainingType;
        }

        return minAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.Protected => "protected",
            Accessibility.Private => "private",
            _ => "internal" // Fallback default
        };
    }

    private static bool IsStricter(Accessibility current, Accessibility baseline)
    {
        // Hierarchical strictness check for C# accessibilities
        return (current, baseline) is
            (Accessibility.Private, _) or
            (Accessibility.ProtectedAndInternal, Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal or Accessibility.Protected) or
            (Accessibility.Protected, Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal) or
            (Accessibility.ProtectedOrInternal, Accessibility.Public or Accessibility.Internal) or
            (Accessibility.Internal, Accessibility.Public);
    }
}
