using ConsoleLog;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Prisms;

namespace Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        Assert.True(true);
    }
}

public class GeneratorTests
{
    [Fact]
    public void GeneratesExpectedOutput()
    {
        var inputTree = CSharpSyntaxTree.ParseText(
            """
            namespace MyNamespace.Nested;



            [Dral.Optics.Generate.Prism]
            record InScrollbar(VerticalScrollbar.FocusElement focusElement) : FocusElement { }

            record InGrid((int column, int row) Element) : FocusElement;
            """,
            cancellationToken: TestContext.Current.CancellationToken
        );
        var references = AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location));

        var compilation = CSharpCompilation.Create(
            "Tests",
            [inputTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        // 2. Run the generator
        var driver = CSharpGeneratorDriver.Create(new PrismGenerator());
        driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics
        );

        // 3. Assert on output
        var generatedCode = outputCompilation.SyntaxTrees.Last().ToString();
        Console.Log($"generatedCode: {generatedCode}");
        Console.Log($"diagnostics: {diagnostics}");
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Contains("class TargetGenerated", generatedCode);
    }
}
