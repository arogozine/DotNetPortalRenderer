namespace BuildAssetLoader.Con
{
    // ===== Mapping Features =====

    // mikesnd — plays the sound numbered by the executing actor's yvel.
    public sealed record MikesndCommand() : Command(CommandList.mikesnd);
}

