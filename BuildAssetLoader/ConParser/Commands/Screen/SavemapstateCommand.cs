namespace BuildAssetLoader.Con
{
    // savemapstate — snapshots the current map's state for a later loadmapstate.
    public sealed record SavemapstateCommand() : Command(CommandList.savemapstate);
}

