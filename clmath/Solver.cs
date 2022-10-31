namespace clmath;

public sealed class Solver
{
    private readonly bool _verbose;

    public Solver(bool verbose)
    {
        _verbose = verbose;
    }

    public Component Solve(Component rhs, Component lhs, string target)
    {
        WalkComponent_Rec(ref rhs, ref lhs, target);
        if (_verbose)
            Console.WriteLine($"{lhs} = {rhs}");
        return lhs;
    }

    private void WalkComponent_Rec(ref Component rhs, ref Component lhs, string target)
    {
        if (rhs.x == null && rhs.y == null)
        {
            // should be unreachable?
            // maybe needed to do nothing
        }
        else
        {
            if (_verbose)
                Console.Write($"{lhs} = {rhs}\t");
            var yCopy = rhs.y?.Copy();
            switch (rhs.type)
            {
                case Component.Type.Op:
                    switch (rhs.op)
                    {
                        case Component.Operator.Add:
                            lhs = new Component
                            {
                                type = Component.Type.Op,
                                op = Component.Operator.Subtract,
                                x = lhs,
                                y = yCopy
                            };
                            if (_verbose)
                                Console.Write(" | -");
                            break;
                        case Component.Operator.Subtract:
                            lhs = new Component
                            {
                                type = Component.Type.Op,
                                op = Component.Operator.Add,
                                x = lhs,
                                y = yCopy
                            };
                            if (_verbose)
                                Console.Write(" | +");
                            break;
                        case Component.Operator.Multiply:
                            lhs = new Component
                            {
                                type = Component.Type.Op,
                                op = Component.Operator.Divide,
                                x = lhs,
                                y = yCopy
                            };
                            if (_verbose)
                                Console.Write(" | /");
                            break;
                        case Component.Operator.Divide:
                            lhs = new Component
                            {
                                type = Component.Type.Op,
                                op = Component.Operator.Multiply,
                                x = lhs,
                                y = yCopy
                            };
                            if (_verbose)
                                Console.Write(" | *");
                            break;
                        case Component.Operator.Power:
                            lhs = new Component
                            {
                                type = Component.Type.Root,
                                x = lhs,
                                y = rhs.y?.Copy() ?? new Component { type = Component.Type.Num, arg = 2d }
                            };
                            if (_verbose)
                                Console.Write(" | sqrt(");
                            break;
                        default:
                            throw new NotSupportedException($"It is impossible to reduce by operator {rhs.op}");
                    }
                    if (_verbose)
                        Console.WriteLine(yCopy);
                    rhs = rhs.x!;
                    break;
                case Component.Type.Root:
                    yCopy ??= new Component { type = Component.Type.Num, arg = 2d };
                    lhs = new Component
                    {
                        type = Component.Type.Op,
                        op = Component.Operator.Power,
                        x = lhs,
                        y = yCopy
                    };
                    rhs = rhs.x!;
                    if (_verbose)
                        Console.Write($" | ^{yCopy}");
                    break;
                case Component.Type.Parentheses:
                    rhs = rhs.x!;
                    WalkComponent_Rec(ref rhs, ref lhs, target);
                    break;
                default:
                    throw new NotSupportedException($"It is impossible to reduce by operation {rhs.type}");
            }
        }
        if (!(rhs.type == Component.Type.Parentheses && rhs.x!.type == Component.Type.Var && (string)rhs.x.arg! == target) 
            && !(rhs.type == Component.Type.Var && (string)rhs.arg! == target))
            WalkComponent_Rec(ref rhs, ref lhs, target);
    }
}