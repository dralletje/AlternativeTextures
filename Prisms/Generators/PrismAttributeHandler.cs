using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Prisms;

static class PrismAttributeHandler
{
    public static IEnumerable<GeneratorEntry> HandlePrism(
        SourceProductionContext ctx,
        INamedTypeSymbol? symbol
    )
    {
        if (symbol is null)
            throw new SourceException("No type");
        var location = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation();
        // Console.Log($"Hey there!");
        var MyType = symbol;
        var OuterType = symbol.BaseType ?? throw new SourceException("No outer type", location);
        var InnerType =
            Helpers.WrapsType(symbol) ?? throw new SourceException("No inner type", location);

        var access = AccessibilityUtility.GetEffectiveAccessibilityKeyword(symbol);

        var props = symbol.GetMembers().OfType<IPropertySymbol>();
        yield return new GeneratorEntry.SourceFile(
            FileName: Helpers.GetUniqueFileName(symbol),
            $$"""
            using Dral.Optics;

            namespace {{symbol.ContainingNamespace}};

            {{access}} static class {{$"{symbol.Name}OpticsExtensions2"}}
            {
                class Prism : IPrism<{{OuterType}}, {{InnerType}}>
                {
                    public bool TryDowncast({{OuterType}} outer, out {{InnerType}} inner)
                    {
                        if (outer is {{MyType}}(var x))
                        {
                            inner = x;
                            return true;
                        }
                        else
                        {
                            inner = default;
                            return false;
                        }
                    }

                    public {{OuterType}} Upcast({{InnerType}} inner) => new {{MyType}}(inner);
                }

                extension({{MyType}} target)
                {
                    public static IPrism<{{OuterType}}, {{InnerType}}> Prism => new Prism();
                }
            }
            """
        );
    }
}
