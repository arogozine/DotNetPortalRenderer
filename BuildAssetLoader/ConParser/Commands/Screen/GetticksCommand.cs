namespace BuildAssetLoader.Con
{
    // ===== Time Access =====

    // getticks <gamevar> — milliseconds since the game started; not synced, for visuals/profiling only.
    public sealed record GetticksCommand(string Gamevar) : Command(CommandList.getticks);
}

