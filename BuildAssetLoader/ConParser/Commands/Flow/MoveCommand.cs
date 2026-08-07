namespace BuildAssetLoader.Con
{
    // ===== Move (declare/invoke) =====

    // move <name> [<horizontal> [<vertical>]] — declaration, placed outside actor/state code.
    // Both velocities are commonly omitted in practice (a zero-velocity "named marker" move), defaulting to 0.
    public sealed record MoveCommand(string Name, int? Horizontal, int? Vertical) : Command(CommandList.move);
}

