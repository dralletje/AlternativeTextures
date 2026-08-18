using System.Drawing;
using ConsoleLog;
using DralGeometry;

namespace PaddingTests;

public class UnitTest1
{
    public static int Add(int x, int y) => x + y;

    [Fact]
    public void Good()
    {
        var padding = new Padding(all: 10) { Left = 20 };
        var rectangle = new Rectangle(x: 100, y: 100, width: 100, height: 100);
        var result = rectangle - padding;
        Console.Log($"padding: {padding * -1}");
        Console.Log($"result: {result}");
    }
}
