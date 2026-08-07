namespace BuildAssetLoader.Con
{
    // ===== Math (display) =====

    // displayrand <gamevar> — random number in [0, 32767]; sync-safe, usable in unsynchronized (display) code.
    public sealed record DisplayrandCommand(string Gamevar) : Command(CommandList.displayrand);
}

