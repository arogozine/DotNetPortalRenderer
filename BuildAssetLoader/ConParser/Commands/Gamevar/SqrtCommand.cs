namespace BuildAssetLoader.Con
{
    // ===== Math Operations =====

    // sqrt <input variable> <output variable>
    public sealed record SqrtCommand(string Input, string Output) : Command(CommandList.sqrt);
}

