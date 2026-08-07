namespace BuildAssetLoader.Con
{
    // ===== Flow Control - If Components =====

    // nullop — no-op, used in place of empty braces.
    public sealed record NullopCommand() : Command(CommandList.nullop);
}

