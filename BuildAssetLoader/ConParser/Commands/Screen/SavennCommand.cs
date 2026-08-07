namespace BuildAssetLoader.Con
{
    public sealed record SavennCommand(string SlotNumber) : BaseSaveCommand(CommandList.savenn, SlotNumber);
}

