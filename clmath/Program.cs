using System.Collections.Immutable;
using Antlr4.Runtime;
using clmath.Antlr;

namespace clmath;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
            StdIoMode();
        else EvalFunc(string.Join(" ", args));
    }

    private static void StdIoMode()
    {
        while (true)
        {
            Console.Write("func> ");
            var func = Console.ReadLine()!;

            switch (func)
            {
                case "exit": return;
                default:
                    EvalFunc(func);
                    break;
            }
        }
    }

    private static void EvalFunc(string f)
    {
        var input = new AntlrInputStream(f);
        var lexer = new MathLexer(input);
        var tokens = new CommonTokenStream(lexer);
        var parser = new MathParser(tokens);
        var func = new MathCompiler().Visit(parser.expr());

        var vars = func.EnumerateVars().Distinct().ToImmutableList();

        if (vars.Count == 0)
        {
            var res = func.Evaluate(null);
            PrintResult(func, res);
        }
        else
        { // obtain vars
            var ctx = new MathContext();
            int i = 0;
            while (i < vars.Count)
            {
                Console.Write($"input {vars[i]}> ");
                var val = double.Parse(Console.ReadLine()!);
                ctx.var[vars[i]] = val;
                i++;
            }

            var res = func.Evaluate(ctx);
            PrintResult(func, res, ctx);
        }
    }

    private static void PrintResult(Component func, double res, MathContext? ctx = null)
    {
        if (ctx == null) // simple x = y
            Console.WriteLine($"\t{func} = {res}");
        else
        { // print with values
            Console.WriteLine($"\t{func}");
            Console.WriteLine("where");
            foreach (var (key, val) in ctx.var)
                Console.WriteLine($"\t{key} = {val}");
            Console.WriteLine($" = {res}");
        }
    }
}

public class MathContext
{
    public readonly Dictionary<string, double> var = new();
}
