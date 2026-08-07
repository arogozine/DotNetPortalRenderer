namespace BuildAssetLoader.Con
{
    // mulscale <Result> <Factor1> <Factor2> <RightShift> — Result = (Factor1 * Factor2) >> RightShift, computed in 64 bits.
    public sealed record MulscaleCommand(string Result, string Factor1, string Factor2, string RightShift) : Command(CommandList.mulscale);
}

