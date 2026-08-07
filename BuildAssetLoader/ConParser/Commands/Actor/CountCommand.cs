namespace BuildAssetLoader.Con
{
    // ===== Actors - Structures (remainder) =====

    // count <number> — sets the actor's count (incremented once per actor code cycle). <number> is commonly a define.
    public sealed record CountCommand(string Number) : Command(CommandList.count);
}

