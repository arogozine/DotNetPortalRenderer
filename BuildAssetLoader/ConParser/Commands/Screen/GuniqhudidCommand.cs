namespace BuildAssetLoader.Con
{
    // guniqhudid <slotID> — selects the animation-state slot (0-254) used for HUD models drawn via rotatesprite.
    public sealed record GuniqhudidCommand(string SlotId) : Command(CommandList.guniqhudid);
}

