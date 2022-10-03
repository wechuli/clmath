using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using clmath.Antlr;

namespace clmath;

public static class Program
{
    private static readonly string Ext = ".math";
    private static readonly string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "comroid", "clmath");
    private static bool _exiting;
    private static bool _viewerAvail;
    private static string _viewer = null!;

    static Program()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }
    
    public static void Main(string[] args)
    {
        _viewer = Path.Combine(Directory.GetParent(typeof(Program).Assembly.Location)!.FullName, "clmath-viewer.exe");
        _viewerAvail = File.Exists(_viewer);
        if (args.Length == 0)
            StdIoMode();
        else
        {
            var arg = string.Join(" ", args);
            if (File.Exists(arg))
                EvalFunc(File.ReadAllText(arg));
            else EvalFunc(arg);
        }
    }

    private static void StdIoMode()
    {
        while (!_exiting)
        {
            Console.Write("math> ");
            var func = Console.ReadLine()!;
            var cmds = func.Split(" ");

            switch (cmds[0])
            {
                case "": break;
                case "exit": return;
                case "help":
                    Console.WriteLine("Available commands:");
                    Console.WriteLine("\thelp\t\tShows this text");
                    Console.WriteLine("\texit\t\tCloses the program");
                    Console.WriteLine("\tload <name>\tLoads function with the given name");
                    Console.WriteLine("\nEnter a function to start evaluating");
                    break;
                case "load":
                    if (cmds.Length == 1)
                    {
                        var funcs = Directory.EnumerateFiles(dir, "*.math").Select(p => new FileInfo(p)).ToArray();
                        if (funcs.Length == 0) 
                            Console.WriteLine("No saved functions");
                        else
                        {
                            Console.WriteLine("Available functions:");
                            foreach (var file in funcs)
                                Console.WriteLine($"\t- {file.Name.Substring(0, file.Name.Length-Ext.Length)}");
                        }
                    } else
                    {
                        var path = Path.Combine(dir, cmds[1] + Ext);
                        if (!File.Exists(path))
                            Console.WriteLine($"Function with name {cmds[1]} not found");
                        else EvalFunc(File.ReadAllText(path));
                    }
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
                else
                {
                    var cmds = cmd.Split(" ");
                    switch (cmds[0])
                    {
                        case "drop": return;
                        case "exit":
                            _exiting = true;
                            return;
                        case "help":
                            Console.WriteLine("Available commands:");
                            Console.WriteLine("\thelp\t\tShows this text");
                            Console.WriteLine("\texit\t\tCloses the program");
                            Console.WriteLine("\tdrop\t\tDrops the current function");
                            Console.WriteLine("\tclear\t\tClears all variables from the cache");
                            Console.WriteLine("\tdump\t\tPrints all variables in the cache");
                            Console.WriteLine("\tsave <name>\tSaves the current function with the given name");
                            if (_viewerAvail)
                                Console.WriteLine("\tgraph\t\tDisplays the function in a 2D graph");
                            Console.WriteLine("\teval\t\tEvaluates the function, also achieved by just pressing return");
                            Console.WriteLine("\nSet variables with an equation (example: 'x = 5' or 'y = x * 2')");
                            break;
                        case "dump":
                            DumpVariables(ctx);
                            break;
                        case "save":
                            var path = Path.Combine(dir, cmds[1] + Ext);
                            File.WriteAllText(path, f);
                            Console.WriteLine($"Function saved as {cmds[1]}");
                            break;
                        case "clear":
                            ctx.var.Clear();
                            break;
                        case "graph":
                            if (vars.Count != 1)
                                Console.WriteLine("Error: Requires exactly 1 variable");
                            else Process.Start(_viewer, f).WaitForExit();
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
