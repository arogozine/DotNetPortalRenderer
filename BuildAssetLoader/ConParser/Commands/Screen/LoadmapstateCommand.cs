namespace BuildAssetLoader.Con
{
    // ===== Hub Maps =====

    // loadmapstate — restores the current map to its last savemapstate snapshot.
    public sealed record LoadmapstateCommand() : Command(CommandList.loadmapstate);
}

