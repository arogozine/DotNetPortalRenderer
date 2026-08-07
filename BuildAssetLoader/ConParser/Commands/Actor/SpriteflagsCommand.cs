namespace BuildAssetLoader.Con
{
    // spriteflags <picnum> <value> (outside actor code, per-tile) or spriteflags <value> (inside actor code, per-sprite).
    // <value> is a bitfield, commonly an SFLAG_* define.
    public sealed record SpriteflagsCommand(string? Picnum, string Value) : Command(CommandList.spriteflags);
}

