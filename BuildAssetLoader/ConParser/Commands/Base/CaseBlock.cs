namespace BuildAssetLoader.Con
{
    /// <summary>One case (or default) block inside a switch. Not a standalone Command —
    /// CommandList.case_/CommandList.default_ are structural markers, consumed here rather than modeled as their own Command types (see §5).</summary>
    public sealed record CaseBlock(int? Constant, bool IsDefault, List<Command> Body);
}

