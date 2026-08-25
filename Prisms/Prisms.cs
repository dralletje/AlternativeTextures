using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
// using ConsoleLog;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Prisms;

[InterpolatedStringHandler]
public ref struct SourceInterpolatedStringHandler(int literalLength, int formattedCount)
{
    private StringBuilder stringBuilder = new();
    private int lastIndentation = 0;

    static int indentation(string str)
    {
        ReadOnlySpan<char> trimmed = str.AsSpan().TrimEnd(' ');
        bool isValid = !trimmed.IsEmpty && (trimmed[^1] == '\n' || trimmed[^1] == '\r');
        int spaceCount = isValid ? str.Length - trimmed.Length : 0;
        return spaceCount;
    }

    public void AppendLiteral(string str)
    {
        lastIndentation = indentation(str);
        stringBuilder.Append(str);
    }

    // public void AppendFormatted<T>(T value)
    // {
    //     stringBuilder.Append(value);
    // }

    public void AppendFormatted(string value)
    {
        stringBuilder.Append(value);
    }

    public void AppendFormatted(IEnumerable<string> strings)
    {
        stringBuilder.Append(string.Join($"\n{new string(' ', lastIndentation)}", strings));
    }

    public void AppendFormatted(INamespaceSymbol symbol)
    {
        /// TODO Format based on where it is being used (e.g. global::...)
        stringBuilder.Append(symbol.ToString());
    }

    public void AppendFormatted(ITypeSymbol symbol)
    {
        stringBuilder.Append(symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    public override string ToString()
    {
        return stringBuilder.ToString();
    }
}

class SourceError(string Message, Location? Location = null) : Exception(Message)
{
    public Location? Location = Location;
}

// public class Prism<Outer, Inner>(Func<Outer, Inner?> downcast, Func<Inner, Outer> upcast): IPrism {
//     public Inner? Downcast(Outer outer) => downcast(outer);
//     public Outer Upcast(Inner inner) => upcast(inner);
// }

[Generator]
public class PrismGenerator : IIncrementalGenerator
{
    static readonly DiagnosticDescriptor ErrorRule = new(
        id: "MYGEN001",
        title: "Invalid Target",
        messageFormat: "Target '{0}' is invalid for generation",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        System.Diagnostics.Debugger.Launch();
        context.RegisterPostInitializationOutput(ctx =>
        {
            // Console.Log($"BRRRRRRRRRRR");

            ctx.AddSource(
                "GenerateLensesAttribute.g.cs",
                SourceText.From(
                    // language=C#
                    """
                    using System;
                    using System.Diagnostics.CodeAnalysis;

                    namespace MyOptics;

                    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
                    public class WithPrismAttribute : Attribute { }

                    public interface IPrism<Outer, Inner>
                    {
                        public bool TryDowncast(Outer outer, [MaybeNullWhen(false)] out Inner inner);
                        public Outer Upcast(Inner inner);
                    }
                    """,
                    Encoding.UTF8
                )
            );
        });

        var provider = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                "MyOptics.WithPrismAttribute",
                predicate: (node, _) => true,
                transform: (ctx, _) => ctx.TargetSymbol as INamedTypeSymbol
            )
            .Where(t => t != null);

        context.RegisterSourceOutput(
            provider,
            (ctx, symbol) =>
            {
                // Console.Log($"!!!!!!!!!!!!!!!!!!!!!!!");
                try
                {
                    var location = symbol
                        .DeclaringSyntaxReferences.FirstOrDefault()
                        ?.GetSyntax()
                        .GetLocation();
                    // Console.Log($"Hey there!");
                    if (symbol is null)
                        throw new SourceError("No type");
                    var MyType = symbol;
                    var OuterType =
                        symbol.BaseType ?? throw new SourceError("No outer type", location);
                    var InnerType =
                        WrapsType(symbol) ?? throw new SourceError("No inner type", location);

                    var access = AccessibilityUtility.GetEffectiveAccessibilityKeyword(symbol);

                    var props = symbol.GetMembers().OfType<IPropertySymbol>();
                    SourceInterpolatedStringHandler src = $$"""
                        using System.Diagnostics.CodeAnalysis;
                        using MyOptics;

                        namespace {{symbol.ContainingNamespace}};

                        {{access}} static class {{$"{symbol.Name}OpticsExtensions"}}
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
                        """;

                    ctx.AddSource(
                        $"{symbol.Name}Lenses.g.cs",
                        SourceText.From(src.ToString(), Encoding.UTF8)
                    );
                }
                catch (SourceError error)
                {
                    // Console.Log($"error: {error}");
                    var diagnostic = Diagnostic.Create(ErrorRule, error.Location, error.Message);
                    ctx.ReportDiagnostic(diagnostic);
                }
                catch (Exception error)
                {
                    // Console.Log($"Error: {error}");
                    throw error;
                }
            }
        );
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
