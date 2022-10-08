using System.Globalization;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using clmath.Antlr;

namespace clmath;

public static class Program
{
    private static readonly string Ext = ".math";

    private static readonly string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "comroid", "clmath");

    private static bool _exiting;
    private static GraphWindow? _graph;

    internal static readonly Dictionary<string, double> constants = new()
    {
        { "pi", Math.PI },
        { "e", Math.E },
        { "tau", Math.Tau }
    };

    static Program()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            StdIoMode();
        }
        else
        {
            if (args[0] == "graph")
            {
                StartGraph(args.ToList().GetRange(1, args.Length - 1).Select(ParseFunc).ToArray());
            }
            else
            {
                var arg = string.Join(" ", args);
                if (File.Exists(arg))
                    EvalFunc(File.ReadAllText(arg));
                else EvalFunc(arg);
            }
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
                    Console.WriteLine("\tlist\t\tLists all loadable functions");
                    Console.WriteLine("\tload <name>\tLoads function with the given name");
                    Console.WriteLine("\tmv <n0> <n1>\tRename function with the given name");
                    Console.WriteLine("\tdelete <name>\tDeletes function with the given name");
                    Console.WriteLine("\tgraph <func..>\tDisplays function/s in a 2D graph");
                    Console.WriteLine("\nEnter a function to start evaluating");
                    break;
                case "list":
                    var funcs = Directory.EnumerateFiles(dir, "*.math").Select(p => new FileInfo(p)).ToArray();
                    if (funcs.Length == 0)
                    {
                        Console.WriteLine("No saved functions");
                    }
                    else
                    {
                        Console.WriteLine("Available functions:");
                        foreach (var file in funcs)
                            Console.WriteLine($"\t- {file.Name.Substring(0, file.Name.Length - Ext.Length)}");
                    }

                    break;
                case "load":
                    if (!IsInvalidArgumentCount(cmds, 2))
                    {
                        var load = LoadFunc(cmds[1]);
                        if (load != null) 
                            EvalFunc(func);
                    }
                    break;
                case "mv" or "rename":
                    if (IsInvalidArgumentCount(cmds, 3))
                        break;
                    var path1 = Path.Combine(dir, cmds[1] + Ext);
                    var path2 = Path.Combine(dir, cmds[2] + Ext);
                    if (!File.Exists(path1))
                        Console.WriteLine($"Function with name {cmds[1]} not found");
                    else File.Move(path1, path2);
                    break;
                case "delete":
                    if (IsInvalidArgumentCount(cmds, 2))
                        break;
                    var path0 = Path.Combine(dir, cmds[1] + Ext);
                    if (File.Exists(path0))
                    {
                        File.Delete(path0);
                        Console.WriteLine($"Function with name {cmds[1]} deleted");
                    }
                    else
                    {
                        Console.WriteLine($"Function with name {cmds[1]} not found");
                    }

                    break;
                case "graph":
                    StartGraph(cmds.ToList().GetRange(1, cmds.Length - 1).Select(ParseFunc).ToArray());
                    break;
                default:
                    EvalFunc(func);
                    break;
            }
        }
    }

    internal static Component? LoadFunc(string name)
    {
        var path = Path.Combine(dir, name + Ext);
        if (!File.Exists(path))
        {
            Console.WriteLine($"Function with name {name} not found");
            return null;
        }
        return ParseFunc(File.ReadAllText(path));
    }

    private static bool IsInvalidArgumentCount(string[] arr, int min)
    {
        if (arr.Length < min)
        {
            Console.WriteLine("Error: Not enough arguments");
            return true;
        }

        return false;
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
        EvalFunc(ParseFunc(f), f);
    }

    private static void EvalFunc(Component func, string? f = null)
    {
        if (!func.EnumerateVars().Distinct().Any())
        {
            var res = func.Evaluate(null);
            PrintResult(func, res);
        }
        else
        {
            // enter editor mode
            var ctx = new MathContext();
            while (true)
            {
                Console.Write($"{func}> ");
                var cmd = Console.ReadLine()!;

                if (Regex.Match(cmd, "([\\w]+)\\s*=\\s*(.+)") is { Success: true } matcher)
                {
                    var key = matcher.Groups[1].Value;
                    var sub = ParseFunc(matcher.Groups[2].Value);
                    if (sub.EnumerateVars().Contains(key))
                        Console.WriteLine($"Error: Variable {key} cannot use itself");
                    else if (constants.ContainsKey(key))
                        Console.WriteLine($"Error: Cannot redefine {key}");
                    else ctx.var[key] = sub;
                }
                else
                {
                    var cmds = cmd.Split(" ");
                    List<string> missing;
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
                            Console.WriteLine("\tgraph\t\tDisplays the function in a 2D graph");
                            Console.WriteLine(
                                "\teval\t\tEvaluates the function, also achieved by just pressing return");
                            Console.WriteLine("\nSet variables with an equation (example: 'x = 5' or 'y = x * 2')");
                            break;
                        case "dump":
                            DumpVariables(ctx);
                            break;
                        case "save":
                            if (IsInvalidArgumentCount(cmds, 2))
                                break;
                            if (f == null)
                            {
                                Console.WriteLine("Error: Cannot save loaded function");
                                break;
                            }
                            var path = Path.Combine(dir, cmds[1] + Ext);
                            File.WriteAllText(path, f);
                            Console.WriteLine($"Function saved as {cmds[1]}");
                            break;
                        case "clear":
                            ctx.var.Clear();
                            break;
                        case "graph":
                            missing = new List<string>();
                            foreach (var var in ctx.var.Values.Append(func).SelectMany(it => it.EnumerateVars())
                                         .Distinct())
                                if (!ctx.var.ContainsKey(var))
                                    missing.Add(var);
                            missing.RemoveAll(constants.ContainsKey);
                            if (missing.Count != 1)
                                Console.WriteLine("Error: Requires exactly 1 variable");
                            StartGraph(func);
                            break;
                        case "eval" or "":
                            missing = new List<string>();
                            foreach (var var in ctx.var.Values.Append(func).SelectMany(it => it.EnumerateVars())
                                         .Distinct())
                                if (!ctx.var.ContainsKey(var))
                                    missing.Add(var);
                            missing.RemoveAll(constants.ContainsKey);
                            if (missing.Count > 0)
                            {
                                DumpVariables(ctx);
                                Console.WriteLine(
                                    $"Error: Missing variable{(missing.Count != 1 ? "s" : "")} {string.Join(", ", missing)}");
                            }
                            else
                            {
                                PrintResult(func, func.Evaluate(ctx), ctx);
                            }

                            break;
                    }
                }
            }
        }
    }

    private static void StartGraph(params Component[] func)
    {
        _graph?.Dispose();
        _graph = new GraphWindow(func);
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