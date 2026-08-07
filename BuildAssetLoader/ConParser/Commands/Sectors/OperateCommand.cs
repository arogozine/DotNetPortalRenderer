namespace BuildAssetLoader.Con
{
    // ===== Sectors - Operating =====

    // operate — the current actor opens a nearby door.
    public sealed record OperateCommand() : Command(CommandList.operate);
}

