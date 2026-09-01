using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Prisms;

public class SourceException(string Message, Location? Location = null) : Exception(Message)
{
    public Location? Location = Location;
}

public record SourceFile(string FileName, SourceText Content)
{
    public SourceFile(string FileName, SourceInterpolatedStringHandler Source)
        : this(FileName, SourceText.From(Source.ToString(), Encoding.UTF8)) { }
}

public abstract record GeneratorEntry
{
    public record SourceFile(Prisms.SourceFile Value) : GeneratorEntry
    {
        public SourceFile(string FileName, SourceText Content)
            : this(new Prisms.SourceFile(FileName, Content)) { }

        public SourceFile(string FileName, SourceInterpolatedStringHandler Source)
            : this(new Prisms.SourceFile(FileName, Source)) { }
    }

    public record Diagnostic(Microsoft.CodeAnalysis.Diagnostic Value) : GeneratorEntry;
}

public static class Diagnostics
{
    public static readonly DiagnosticDescriptor GenericErrorRule = new(
        id: "PRISM001",
        title: "Invalid Target",
        messageFormat: "Target '{0}' is invalid for generation",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}

[Generator]
public class PrismGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        System.Diagnostics.Debugger.Launch();
        context.RegisterPostInitializationOutput(ctx =>
        {
            // Console.Log($"BRRRRRRRRRRR");

            ctx.AddSource(
                "Dral.Optics.Generate.cs",
                SourceText.From(
                    // language=C#
                    """
                    using System;
                    using System.Diagnostics.CodeAnalysis;

                    namespace Dral.Optics.Generate;

                    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
                    public class PrismAttribute : Attribute { }

                    public interface IPrism<Outer, Inner>
                    {
                        public bool TryDowncast(Outer outer, [MaybeNullWhen(false)] out Inner inner);
                        public Outer Upcast(Inner inner);
                    }
                    """,
                    Encoding.UTF8
                )
            );
            ctx.AddSource(
                "Dral.Optics.cs",
                SourceText.From(
                    // language=C#
                    """
                    using System;

                    namespace Dral.Optics;

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
                try
                {
                    foreach (var entry in PrismAttributeHandler.HandlePrism(ctx, symbol))
                    {
                        switch (entry)
                        {
                            case GeneratorEntry.SourceFile file:
                                ctx.AddSource(file.FileName, file.Content);
                                break;
                            case GeneratorEntry.Diagnostic diagnostic:
                                ctx.ReportDiagnostic(diagnostic.Value);
                                break;
                        }
                    }
                }
                catch (SourceException error)
                {
                    var diagnostic = Diagnostic.Create(
                        Diagnostics.GenericErrorRule,
                        error.Location,
                        error.Message
                    );
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
}
