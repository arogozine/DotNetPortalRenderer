namespace BuildAssetLoader.Con
{
    // flash — clears the level's visibility briefly and darkens the sprite, like EXPLOSION2.
    public sealed record FlashCommand() : Command(CommandList.flash);
}

