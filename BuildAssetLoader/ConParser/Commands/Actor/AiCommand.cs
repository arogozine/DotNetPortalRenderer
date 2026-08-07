namespace BuildAssetLoader.Con
{
    // ===== Global Settings - Subroutines helpers (ai declare/invoke) =====

    public sealed record AiCommand(string Name, string? Action, string? Move, string[]? MoveFlag)
        : Command(CommandList.ai);
}

