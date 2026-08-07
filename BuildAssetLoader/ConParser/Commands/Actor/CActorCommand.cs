namespace BuildAssetLoader.Con
{
    // ===== Actors - Structures =====

    public sealed record CActorCommand(string Name) : Command(CommandList.cactor);
}

