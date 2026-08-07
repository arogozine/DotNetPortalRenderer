namespace BuildAssetLoader.Con
{
    public sealed record SaveCommand(string SlotNumber) : BaseSaveCommand(CommandList.save, SlotNumber);
}

