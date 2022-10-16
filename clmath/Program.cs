using System.Globalization;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using clmath.Antlr;

namespace clmath;

public static class Program
{
    private static readonly string FuncExt = ".math";
    private static readonly string ConstExt = ".vars";

    private static readonly string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "comroid", "clmath");

    private static readonly string constantsFile = Path.Combine(dir, "constants" + ConstExt);

    private static bool _exiting;
    private static Graph? _graph;
    private static readonly Stack<(Component func, MathContext ctx)> stash = new();

    private static readonly Dictionary<string, double> globalConstants = new()
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
        if (!File.Exists(constantsFile))
            SaveConstants(new Dictionary<string, double>());
        LoadConstants();
    }

    internal static Dictionary<string, double> constants { get; private set; } = null!;

    private static void SaveConstants(Dictionary<string, double>? values = null)
    {
        values ??= constants;
        File.WriteAllText(constantsFile, ConvertValuesToString(values, globalConstants.ContainsKey));
    }

    private static void LoadConstants()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (constants == null)
            constants = new Dictionary<string, double>();
        else constants.Clear();
        foreach (var (key, value) in globalConstants)
            constants[key] = value;
        foreach (var (key, value) in ConvertValuesFromString(File.ReadAllText(constantsFile)))
            constants[key] = value.Evaluate(null);
    }

    private static string ConvertValuesToString(Dictionary<string, Component> values, Func<string, bool>? skip = null)
    {
        skip ??= _ => false;
        var txt = string.Empty;
        foreach (var (key, value) in values)
            if (!skip(key))
                txt += $"{key} = {value}\n";
        return txt;
    }

    private static string ConvertValuesToString(Dictionary<string, double> values, Func<string, bool>? skip = null)
    {
        skip ??= _ => false;
        var txt = string.Empty;
        foreach (var (key, value) in values)
            if (!skip(key))
                txt += $"{key} = {value}\n";
        return txt;
    }

    private static Dictionary<string, Component> ConvertValuesFromString(string data)
    {
        Dictionary<string, Component> vars = new();
        foreach (var (key, value) in data.Replace("\r\n", "\n").Split("\n")
                     .Select(ConvertValueFromString)
                     .Where(e => e.HasValue)
                     .Select(e => e!.Value))
            vars[key] = value;
        return vars;
    }

    private static (string key, Component value)? ConvertValueFromString(string data)
    {
        if (Regex.Match(data, "([\\w]+)\\s*=\\s*(.+)") is not { Success: true } matcher)
            return null;
        var key = matcher.Groups[1].Value;
        var value = ParseFunc(matcher.Groups[2].Value);
        return (key, value);
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
                StartGraph(CreateArgsFuncs(args));
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

    private static string CleanupString(string str)
    {
        var leadingSpaces = 0;
        for (var i = 0; i < str.Length && str[i] == ' '; i++)
            leadingSpaces++;
        return str.Substring(leadingSpaces, str.Length - leadingSpaces);
    }

    private static void StdIoMode()
    {
        while (!_exiting)
        {
            Console.Write("math> ");
            var func = Console.ReadLine()!;
            func = CleanupString(func);
            var cmds = func.Split(" ");

            switch (cmds[0])
            {
                case "": break;
                case "exit": return;
                case "help":
                    Console.WriteLine($"clmath v{typeof(Program).Assembly.GetName().Version} by comroid\n");
                    Console.WriteLine("Available commands:");
                    Console.WriteLine("\thelp\t\tShows this text");
                    Console.WriteLine("\texit\t\tCloses the program");
                    Console.WriteLine("\tset <const>\tDefines a constant");
                    Console.WriteLine("\tunset <const>\tRemoves a constant");
                    Console.WriteLine("\tlist <target>\tLists things");
                    Console.WriteLine("\tload <name>\tLoads function with the given name");
                    Console.WriteLine("\tmv <n0> <n1>\tRename function with the given name");
                    Console.WriteLine("\tdelete <name>\tDeletes function with the given name");
                    Console.WriteLine("\trestore <trace>\tRestores a function from stash");
                    Console.WriteLine("\tgraph <func..>\tDisplays function/s in a 2D graph");
                    Console.WriteLine("\nEnter a function to start evaluating");
                    break;
                case "set":
                    var setConstN = ConvertValueFromString(func.Substring("set ".Length, func.Length - "set ".Length));
                    if (setConstN is not { } setConst)
                    {
                        Console.WriteLine("Error: Invalid declaration of constant variable; try 'x = 5'");
                        break;
                    }

                    if (globalConstants.ContainsKey(setConst.key))
                    {
                        Console.WriteLine($"Error: Cannot redefine {setConst.key}");
                        break;
                    }

                    constants[setConst.key] = setConst.value.Evaluate(null);
                    SaveConstants();
                    break;
                case "unset":
                    if (IsInvalidArgumentCount(cmds, 2))
                        break;
                    if (globalConstants.ContainsKey(cmds[1]))
                    {
                        Console.WriteLine($"Error: Cannot unset {cmds[1]}");
                        break;
                    }

                    if (!constants.ContainsKey(cmds[1]))
                    {
                        Console.WriteLine($"Error: Unknown constant {cmds[1]}");
                        break;
                    }

                    constants.Remove(cmds[1]);
                    SaveConstants();
                    break;
                case "list":
                    if (cmds.Length == 1)
                    {
                        Console.WriteLine("Error: Listing target unspecified; options are 'funcs' and 'constants'");
                        break;
                    }

                    switch (cmds[1])
                    {
                        case "funcs" or "fx":
                            var funcs = Directory.EnumerateFiles(dir, "*.math").Select(p => new FileInfo(p)).ToArray();
                            if (funcs.Length == 0)
                            {
                                Console.WriteLine("No saved functions");
                            }
                            else
                            {
                                Console.WriteLine("Available functions:");
                                foreach (var file in funcs)
                                    Console.WriteLine(
                                        $"\t- {file.Name.Substring(0, file.Name.Length - FuncExt.Length)}");
                            }

                            break;
                        case "constants" or "const":
                            if (constants.Count == 0)
                            {
                                Console.WriteLine("No available constants");
                            }
                            else
                            {
                                Console.WriteLine("Available constants:");
                                foreach (var (key, value) in constants)
                                    Console.WriteLine($"\t{key}\t= {value}");
                            }

                            break;
                        case "stash":
                            if (stash.Count == 0)
                            {
                                Console.WriteLine("No functions in stash");
                            }
                            else
                            {
                                Console.WriteLine("Stashed Functions:");
                                var i = 0;
                                foreach (var (fx, ctx) in stash)
                                {
                                    Console.WriteLine($"\tstash[{i++}]\t= {fx}");
                                    ctx.DumpVariables("stash[#]".Length / 8 + 1, false);
                                }
                            }

                            break;
                        default:
                            Console.WriteLine(
                                $"Error: Unknown listing target '{cmds[1]}';  options are 'funcs', 'constants' and 'stash'");
                            break;
                    }

                    break;
                case "load":
                    if (!IsInvalidArgumentCount(cmds, 2))
                    {
                        var load = LoadFunc(cmds[1]);
                        if (load is { } res)
                            EvalFunc(res.func, ctx: res.ctx);
                    }

                    break;
                case "mv" or "rename":
                    if (IsInvalidArgumentCount(cmds, 3))
                        break;
                    var path1 = Path.Combine(dir, cmds[1] + FuncExt);
                    var path2 = Path.Combine(dir, cmds[2] + FuncExt);
                    if (!File.Exists(path1))
                        Console.WriteLine($"Function with name {cmds[1]} not found");
                    else File.Move(path1, path2);
                    break;
                case "rm" or "delete":
                    if (IsInvalidArgumentCount(cmds, 2))
                        break;
                    var path0 = Path.Combine(dir, cmds[1] + FuncExt);
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
                case "restore":
                    (Component func, MathContext ctx) entry;
                    if (cmds.Length == 1)
                    {
                        entry = stash.Pop();
                    }
                    else
                    {
                        if (Regex.Match(cmds[1], "\\d+") is { Success: true })
                        {
                            var index = int.Parse(cmds[1]);
                            if (index > stash.Count)
                            {
                                Console.WriteLine($"Error: Backtrace index {index} too large");
                                break;
                            }

                            entry = stash.ToArray()[index];
                            var bak = stash.ToList();
                            bak.Remove(entry);
                            stash.Clear();
                            bak.Reverse();
                            bak.ForEach(stash.Push);
                        }
                        else
                        {
                            Console.WriteLine($"Error: Invalid backtrace {cmds[1]}");
                            break;
                        }
                    }

                    EvalFunc(entry.func, ctx: entry.ctx);
                    break;
                case "graph":
                    StartGraph(cmds.Length == 1 ? stash.ToArray() : CreateArgsFuncs(cmds));
                    break;
                default:
                    EvalFunc(func);
                    break;
            }
        }
    }

    private static (Component, MathContext)[] CreateArgsFuncs(params string[] args)
    {
        return args.ToList()
            .GetRange(1, args.Length - 1)
            .Select(ParseFunc)
            .Select(fx => (fx, new MathContext()))
            .ToArray();
    }

    internal static (Component func, MathContext ctx)? LoadFunc(string name)
    {
        var path = Path.Combine(dir, name + FuncExt);
        if (!File.Exists(path))
        {
            Console.WriteLine($"Function with name {name} not found");
            return null;
        }

        var data = File.ReadAllText(path);
        var lnb = data.IndexOf("\n", StringComparison.Ordinal);
        MathContext ctx;
        if (lnb != -1)
        {
            var vars = ConvertValuesFromString(data.Substring(lnb + 1, data.Length - lnb - 2));
            ctx = new MathContext(vars);
        }
        else
        {
            ctx = new MathContext();
        }

        return (ParseFunc(lnb == -1 ? data : data.Substring(0, lnb)), ctx);
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

    private static Component ParseFunc(string f)
    {
        var input = new AntlrInputStream(f);
        var lexer = new MathLexer(input);
        var tokens = new CommonTokenStream(lexer);
        var parser = new MathParser(tokens);
        return new MathCompiler().Visit(parser.expr());
    }

    private static void EvalFunc(string f)
    {
        var fx = ParseFunc(f);
        EvalFunc(fx, fx.ToString());
    }

    private static void EvalFunc(Component func, string? f = null, MathContext? ctx = null)
    {
        if (func.EnumerateVars().Distinct().All(constants.ContainsKey))
        {
            var res = func.Evaluate(null);
            PrintResult(func, res);
        }
        else
        {
            // enter editor mode
            ctx ??= new MathContext();
            while (true)
            {
                Console.Write($"{func}> ");
                var cmd = Console.ReadLine()!;
                cmd = CleanupString(cmd);

                if (ConvertValueFromString(cmd) is { } result)
                {
                    var key = result.key;
                    var value = result.value;
                    if (value.EnumerateVars().Contains(key))
                        Console.WriteLine($"Error: Variable {key} cannot use itself");
                    else if (constants.ContainsKey(key))
                        Console.WriteLine($"Error: Cannot redefine {key}");
                    else ctx.var[key] = value;

                    if (FindMissingVariables(func, ctx).Count == 0)
                        PrintResult(func, func.Evaluate(ctx));
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
                            Console.WriteLine($"clmath v{typeof(Program).Assembly.GetName().Version} by comroid\n");
                            Console.WriteLine("Available commands:");
                            Console.WriteLine("\thelp\t\tShows this text");
                            Console.WriteLine("\texit\t\tCloses the program");
                            Console.WriteLine("\tdrop\t\tDrops the current function");
                            Console.WriteLine("\tclear [var]\tClears all variables or just one from the cache");
                            Console.WriteLine("\tdump\t\tPrints all variables in the cache");
                            Console.WriteLine(
                                "\tsave <name>\tSaves the current function with the given name; append '-y' to store current variables as well");
                            Console.WriteLine("\tstash\t\tStores the function in stash");
                            Console.WriteLine("\tgraph\t\tDisplays the function in a 2D graph");
                            Console.WriteLine(
                                "\teval\t\tEvaluates the function, also achieved by just pressing return");
                            Console.WriteLine("\nSet variables with an equation (example: 'x = 5' or 'y = x * 2')");
                            break;
                        case "dump":
                            DumpVariables(ctx, func.ToString().Length / 8 + 1);
                            break;
                        case "save":
                            if (IsInvalidArgumentCount(cmds, 2))
                                break;
                            var data = f ?? func.ToString();
                            if (cmds.Length > 2 && cmds[2] == "-y")
                                data += $"\n{ConvertValuesToString(ctx.var, globalConstants.ContainsKey)}";
                            var path = Path.Combine(dir, cmds[1] + FuncExt);
                            File.WriteAllText(path, data);
                            Console.WriteLine($"Function saved as {cmds[1]}");
                            break;
                        case "clear":
                            if (cmds.Length > 1)
                            {
                                if (!ctx.var.ContainsKey(cmds[1]))
                                {
                                    Console.WriteLine($"Error: Variable {cmds[1]} not found");
                                    break;
                                }

                                ctx.var.Remove(cmds[1]);
                                Console.WriteLine($"Variable {cmds[1]} deleted");
                            }
                            else
                            {
                                ctx.var.Clear();
                            }

                            break;
                        case "stash":
                            stash.Push((func, ctx));
                            return;
                        case "graph":
                            stash.Push((func, ctx));
                            StartGraph(stash.ToArray());
                            stash.Pop();
                            break;
                        case "eval":
                            var missing = FindMissingVariables(func, ctx);
                            if (missing.Count > 0)
                            {
                                DumpVariables(ctx, func.ToString().Length / 8 + 1);
                                Console.WriteLine(
                                    $"Error: Missing variable{(missing.Count != 1 ? "s" : "")} {string.Join(", ", missing)}");
                            }
                            else
                            {
                                PrintResult(func, func.Evaluate(ctx), ctx);
                            }

                            break;
                        default:
                            Console.WriteLine("Error: Unknown command; type 'help' for a list of commands");
                            break;
                    }
                }
            }
        }
    }

    private static List<string> FindMissingVariables(Component func, MathContext ctx)
    {
        var missing = new List<string>();
        foreach (var var in ctx.var.Values.Append(func).SelectMany(it => it.EnumerateVars())
                     .Distinct())
            if (!ctx.var.ContainsKey(var))
                missing.Add(var);
        missing.RemoveAll(constants.ContainsKey);
        return missing;
    }

    private static void StartGraph(params (Component fx, MathContext ctx)[] funcs)
    {
        _graph?.Dispose();
        _graph = new Graph(funcs);
    }

    private static int DumpVariables(this MathContext ctx, int alignBase = 1, bool shouldError = true)
    {
        if (ctx.var.Count == 0)
        {
            if (shouldError)
                Console.WriteLine("Error: No variables are set");
            return 1;
        }

        var maxAlign = ctx.var.Keys.Max(key => key.Length) / 8;
        foreach (var (key, val) in ctx.var)
        {
            var align = Math.Max(maxAlign > 0 ? maxAlign - alignBase : alignBase,
                maxAlign - (key.Length / 8 + 1) + alignBase);
            var spacer = Enumerable.Range(0, align).Aggregate(string.Empty, (str, _) => str + '\t');
            Console.WriteLine($"\t{key}{spacer}= {val}");
        }

        return maxAlign;
    }

    private static void PrintResult(Component func, double res, MathContext? ctx = null)
    {
        var funcAlign = func.ToString().Length / 8 + 1;
        var align = Math.Max(1, (ctx?.DumpVariables(funcAlign) ?? 1) - funcAlign);
        var spacer = Enumerable.Range(0, align).Aggregate(string.Empty, (str, _) => str + '\t');
        Console.WriteLine($"\t{func}{spacer}= {res}");
    }
}

public sealed class MathContext
{
    public readonly Dictionary<string, Component> var = new();

    public MathContext() : this((MathContext?)null)
    {
    }

    public MathContext(MathContext? copy) : this(copy?.var)
    {
    }

    public MathContext(Dictionary<string, Component>? copy)
    {
        if (copy != null)
            foreach (var (key, value) in copy)
                var[key] = value;
    }
}