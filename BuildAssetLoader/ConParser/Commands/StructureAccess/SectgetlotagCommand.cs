namespace BuildAssetLoader.Con
{
    // sectgetlotag — deprecated; current sector's lotag into per-actor gamevar LOTAG.
    public sealed record SectgetlotagCommand() : Command(CommandList.sectgetlotag);
}

