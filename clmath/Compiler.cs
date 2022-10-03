using Antlr4.Runtime.Tree;
using clmath.Antlr;

namespace clmath;

public class MathCompiler : MathBaseVisitor<Component>
{
    public override Component VisitNum(MathParser.NumContext context) =>
        new() { type = Component.Type.Num, arg = double.Parse(context.GetText()) };

    public override Component VisitWord(MathParser.WordContext context) =>
        new() { type = Component.Type.Var, arg = context.GetText() };

    public override Component VisitFrac(MathParser.FracContext context) => new()
        { type = Component.Type.Frac, x = Visit(context.x), y = Visit(context.y) };

    public override Component VisitFx(MathParser.FxContext context) => new()
    {
        type = Component.Type.FuncX,
        func = context.func().Start.Type switch
        {
            MathLexer.SIN => Component.FuncX.Sin,
            MathLexer.COS => Component.FuncX.Cos,
            MathLexer.TAN => Component.FuncX.Tan,
            MathLexer.LOG => Component.FuncX.Log,
            MathLexer.SEC => Component.FuncX.Sec,
            MathLexer.CSC => Component.FuncX.Csc,
            MathLexer.COT => Component.FuncX.Cot,
            MathLexer.HYP => Component.FuncX.Hyp,
            MathLexer.ARCSIN => Component.FuncX.ArcSin,
            MathLexer.ARCCOS => Component.FuncX.ArcCos,
            MathLexer.ARCTAN => Component.FuncX.ArcTan,
            _ => throw new NotSupportedException(context.func().GetText())
        },
        x = Visit(context.x)
    };

    public override Component VisitRoot(MathParser.RootContext context) => new()
        { type = Component.Type.Root, x = Visit(context.x), y = Visit(context.i) };

    public override Component VisitExprOp(MathParser.ExprOpContext context) => new()
    {
        type = Component.Type.Op,
        op = context.op().Start.Type switch
        {
            MathLexer.OP_ADD => Component.Operator.Add,
            MathLexer.OP_SUB => Component.Operator.Subtract,
            MathLexer.OP_MUL => Component.Operator.Multiply,
            MathLexer.OP_DIV => Component.Operator.Divide,
            MathLexer.OP_MOD => Component.Operator.Modulus,
            MathLexer.POW => Component.Operator.Power,
            _ => throw new NotSupportedException(context.op().GetText())
        },
        x = Visit(context.l),
        y = Visit(context.r)
    };

    public override Component Visit(IParseTree? tree) => (tree == null ? null : base.Visit(tree))!;

    protected override bool ShouldVisitNextChild(IRuleNode node, Component? currentResult) => currentResult == null;
}

public sealed class Component
{
    public Type type { get; init; }
    public FuncX? func { get; init; }
    public Operator? op { get; init; }
    public Component? x { get; init; }
    public Component? y { get; init; }
    public object? arg { get; set; }

    public List<string> EnumerateVars()
    {
        if (type == Type.Var)
            return new List<string>() { (arg as string)! };
        List<string> vars = new();
        x?.EnumerateVars().ForEach(vars.Add);
        y?.EnumerateVars().ForEach(vars.Add);
        return vars;
    }

    public double Evaluate(MathContext? ctx)
    {
        var x = this.x?.Evaluate(ctx);
        var y = this.y?.Evaluate(ctx);
        switch (type)
        {
            case Type.Num:
                return (double)arg!;
            case Type.Var:
                return ctx!.var[(string)arg!].Evaluate(ctx);
            case Type.FuncX:
                switch (func)
                {
                    case FuncX.Sin:
                        return Math.Sin(x!.Value);
                    case FuncX.Cos:
                        return Math.Cos(x!.Value);
                    case FuncX.Tan:
                        return Math.Tan(x!.Value);
                    case FuncX.Log:
                        return Math.Log(x!.Value);
                    case FuncX.Sec:
                        //return Math.Sec(x!.Value);
                        break;
                    case FuncX.Csc:
                        //return Math.Csc(x!.Value);
                        break;
                    case FuncX.Cot:
                        //return Math.Cot(x!.Value);
                        break;
                    case FuncX.Hyp:
                        //return Math.Hyp(x!.Value);
                        break;
                    case FuncX.ArcSin:
                        return Math.Asin(x!.Value);
                    case FuncX.ArcCos:
                        return Math.Acos(x!.Value);
                    case FuncX.ArcTan:
                        return Math.Atan(x!.Value);
                    case null: throw new Exception("invalid state");
                }
                break;
            case Type.Root:
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                return Math.Pow(x!.Value, 1 / (y ?? 2d));
            case Type.Frac:
                return x!.Value / y!.Value;
            case Type.Op:
                switch (op)
                {
                    case Operator.Add:
                        return x!.Value + y!.Value;
                    case Operator.Subtract:
                        return x!.Value - y!.Value;
                    case Operator.Multiply:
                        return x!.Value * y!.Value;
                    case Operator.Divide:
                        return x!.Value / y!.Value;
                    case Operator.Modulus:
                        return x!.Value % y!.Value;
                    case Operator.Power:
                        return Math.Pow(x!.Value, y!.Value);
                    case null: throw new Exception("invalid state");
                }
                break;
        }

        throw new NotSupportedException(this.ToString());
    }

    public override string ToString()
    {
        switch (type)
        {
            case Type.Num:
            case Type.Var:
                return arg!.ToString()!;
            case Type.FuncX:
                return $"{func.ToString()!.ToLower()}({x})";
            case Type.Root:
                var n = y?.ToString() ?? "2";
                return $"{(n == "2" ? "sqrt" : $"root[{n}]")}({x})";
            case Type.Frac:
                return $"frac({x})({y})";
            case Type.Op:
                var op = this.op switch
                {
                    Operator.Add => '+',
                    Operator.Subtract => '-',
                    Operator.Multiply => '*',
                    Operator.Divide => '/', 
                    Operator.Modulus => '%',
                    Operator.Power => '^',
                    _ => throw new ArgumentOutOfRangeException()
                };
                return $"{x}{op}{y}";
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    public enum Type
    {
        Num,
        Var,
        FuncX,
        Root,
        Frac,
        Op
    }

    public enum FuncX
    {
        Sin,
        Cos,
        Tan,
        Log,
        Sec,
        Csc,
        Cot,
        Hyp,
        ArcSin,
        ArcCos,
        ArcTan
    }

    public enum Operator
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulus,
        Power
    }
}
