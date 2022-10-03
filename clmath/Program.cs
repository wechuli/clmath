using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using clmath.Antlr;

namespace clmath;

public static class Program
{
    private static bool _exiting;
    private static bool _viewerAvail;
    private static string _viewer = null!;

    public static void Main(string[] args)
    {
        _viewer = Path.Combine(Directory.GetParent(typeof(Program).Assembly.Location)!.FullName, "clmath-viewer.exe");
        _viewerAvail = File.Exists(_viewer);
        if (args.Length == 0)
            StdIoMode();
        else EvalFunc(string.Join(" ", args));
    }

    private static void StdIoMode()
    {
        while (!_exiting)
        {
            Console.Write("func> ");
            var func = Console.ReadLine()!;

            switch (func)
            {
                case "": break;
                case "exit": return;
                case "help":
                    Console.WriteLine("Available commands:");
                    Console.WriteLine("\thelp\tShows this text");
                    Console.WriteLine("\texit\tCloses the program");
                    Console.WriteLine("\nEnter a function to start evaluating");
                    break;
                default:
                    EvalFunc(func);
                    break;
            }
        }
    }

    public static Component ParseFunc(string f)
    {
        var input = new AntlrInputStream(f);
        var lexer = new MathLexer(input);
        var tokens = new CommonTokenStream(lexer);
        var parser = new MathParser(tokens);
        return new MathCompiler().Visit(parser.expr());
    }

    private static void EvalFunc(string f)
    {
        var func = ParseFunc(f);
        var vars = func.EnumerateVars().Distinct().ToImmutableList();

        if (vars.Count == 0)
        {
            var res = func.Evaluate(null);
            PrintResult(func, res);
        }
        else
        { // enter editor mode
            var ctx = new MathContext();
            while (true)
            {
                Console.Write($"{func}> ");
                var cmd = Console.ReadLine()!;

                if (Regex.Match(cmd, "([\\w])+\\s*=\\s*(.+)") is { Success: true } matcher)
                {
                    var key = matcher.Groups[1].Value;
                    var sub = ParseFunc(matcher.Groups[2].Value);
                    if (sub.EnumerateVars().Contains(key))
                        Console.WriteLine($"Error: Variable {key} cannot use itself");
                    else ctx.var[key] = sub;
                }
                else switch (cmd)
                {
                    case "drop": return;
                    case "exit":
                        _exiting = true;
                        return;
                    case "help":
                        Console.WriteLine("Available commands:");
                        Console.WriteLine("\thelp\tShows this text");
                        Console.WriteLine("\texit\tCloses the program");
                        Console.WriteLine("\tdrop\tDrops the current function");
                        Console.WriteLine("\tclear\tClears all variables from the cache");
                        Console.WriteLine("\tdump\tPrints all variables in the cache");
                        if (_viewerAvail)
                            Console.WriteLine("\tgraph\tDisplays the function in a 2D graph");
                        Console.WriteLine("\teval\tEvaluates the function, also achieved by just pressing return");
                        Console.WriteLine("\nSet variables with an equation (example: 'x = 5' or 'y = x * 2')");
                        break;
                    case "dump":
                        DumpVariables(ctx);
                        break;
                    case "clear":
                        ctx.var.Clear();
                        break;
                    case "graph":
                        if (vars.Count != 1)
                            Console.WriteLine("Error: Requires exactly 1 variable");
                        else Process.Start(_viewer, func.ToString()).WaitForExit();
                        break;
                    case "eval" or "":
                        List<string> missing = new();
                        foreach (var var in vars)
                            if (!ctx.var.ContainsKey(var))
                                missing.Add(var);
                        if (missing.Count > 0)
                        {
                            DumpVariables(ctx);
                            Console.WriteLine($"Missing variable{(missing.Count != 1 ? "s" : "")} {string.Join(", ", missing)}");
                        } else PrintResult(func, func.Evaluate(ctx), ctx);
                        break;
                }
            }
        }
    }

    private static void DumpVariables(this MathContext ctx)
    {
        foreach (var (key, val) in ctx.var)
            Console.WriteLine($"\t{key} = {val}");
    }

    private static void PrintResult(Component func, double res, MathContext? ctx = null)
    {
        ctx?.DumpVariables();
        Console.WriteLine($"\t{func} = {res}");
    }
}

public sealed class MathContext
{
    public readonly Dictionary<string, Component> var = new();
}
