// namespace Incubator.MonoGame;

// public enum Operator
// {
//     Plus,
//     Minus,
// }

// public closed record Calc
// {
//     public record Px(int pixels) : Calc;

//     public record Vw(float viewportWidth) : Calc;

//     public record Vh(float viewportHeight) : Calc;

//     public record Pc(float percentage) : Calc;

//     public record Binary(Calc a, Operator @operator, Calc b) : Calc;

//     ///
//     public static Calc operator +(Calc a, Calc b) => new Binary(a, Operator.Plus, b);

//     public static Calc operator -(Calc a, Calc b) => new Binary(a, Operator.Minus, b);

//     public static Calc operator *(Calc calculation, float f) =>
//         calculation switch
//         {
//             Px(var pixels) => new Px((int)(pixels * f)),
//             Vw(var viewportWidth) => new Vw(viewportWidth * f),
//             Vh(var viewportHeight) => new Vh(viewportHeight * f),
//             Pc(var percentage) => new Pc(percentage * f),
//             Binary(var a, var @operator, var b) => new Binary(a * f, @operator, b * f),
//         };

//     public static Calc operator /(Calc calculation, int f) => calculation * (1 / f);

//     public int Calculate(int pc, int vh, int vw) =>
//         this switch
//         {
//             Px(var amount) => amount,
//             Vw(var fraction) => (int)(fraction * vw),
//             Vh(var fraction) => (int)(fraction * vh),
//             Pc(var fraction) => (int)(fraction * pc),
//             Binary(var a, var @operator, var b) => ApplyOperator(
//                 a.Calculate(pc, vh, vw),
//                 @operator,
//                 b.Calculate(pc, vh, vw)
//             ),
//         };

//     public static Calc Zero = new Calc.Px(0);

//     static int ApplyOperator(int a, Operator @operator, int b) =>
//         @operator switch
//         {
//             Operator.Plus => a + b,
//             Operator.Minus => a - b,
//         };

//     public static implicit operator Calc(int pixels) => new Calc.Px(pixels);
// }

// public static class CalcExtensions
// {
//     extension(int amount)
//     {
//         public Calc Px => new Calc.Px(amount);
//     }

//     extension(float amount)
//     {
//         public Calc Pc => new Calc.Pc(amount);
//         public Calc Vw => new Calc.Vw(amount);
//         public Calc Vh => new Calc.Vh(amount);
//     }
// }
