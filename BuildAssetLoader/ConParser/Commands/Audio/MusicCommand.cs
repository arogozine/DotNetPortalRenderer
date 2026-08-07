namespace BuildAssetLoader.Con
{
    // ===== Music =====

    // music <volume> <level 1> [level 2] ... [level MAXLEVELS] — declarative, placed outside actor/event code.
    public sealed record MusicCommand(int Volume, string[] Levels) : Command(CommandList.music);
}

