using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class Helpers
{
    public static string GetUniqueFileName(ISymbol symbol)
    {
        var fqn = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var sanitized = fqn.Replace("global::", "").Replace('<', '[').Replace('>', ']');

        // using var sha = SHA256.Create();
        // var hash = BitConverter
        //     .ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(fqn)))
        //     .Replace("-", "")
        //     .Substring(0, 8);

        // return $"{sanitized}_{hash}.g.cs";
        return $"{sanitized}.g.cs";
    }

    public static bool IsPrimaryConstructor(IMethodSymbol method) =>
        method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is TypeDeclarationSyntax;

    public static ITypeSymbol? WrapsType(INamedTypeSymbol typeSymbol)
    {
        var primaryCtor = typeSymbol.Constructors.FirstOrDefault(c =>
            c.Parameters.Length == 1 && IsPrimaryConstructor(c)
        );

        if (primaryCtor == null)
            return null;

        var expectedType = primaryCtor.Parameters[0].Type;

        var hasDeconstruct = typeSymbol
            .GetMembers("Deconstruct")
            .OfType<IMethodSymbol>()
            .Any(m =>
                m.Parameters.Length == 1
                && m.Parameters[0].RefKind == RefKind.Out
                && SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, expectedType)
            );

        if (!hasDeconstruct)
            return null;

        return expectedType;
    }
}

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
            _ => "internal", // Fallback default
        };
    }

    private static bool IsStricter(Accessibility current, Accessibility baseline)
    {
        // Hierarchical strictness check for C# accessibilities
        return (current, baseline)
            is
                (Accessibility.Private, _)
                or
                (
                    Accessibility.ProtectedAndInternal,
                    Accessibility.Public
                        or Accessibility.Internal
                        or Accessibility.ProtectedOrInternal
                        or Accessibility.Protected
                )
                or
                (
                    Accessibility.Protected,
                    Accessibility.Public
                        or Accessibility.Internal
                        or Accessibility.ProtectedOrInternal
                )
                or
                (Accessibility.ProtectedOrInternal, Accessibility.Public or Accessibility.Internal)
                or
                (Accessibility.Internal, Accessibility.Public);
    }
}
