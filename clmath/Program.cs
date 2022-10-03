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
        throw new NotImplementedException();
    }

    private static void EvalFunc(string f)
    {
        throw new NotImplementedException();
    }
}