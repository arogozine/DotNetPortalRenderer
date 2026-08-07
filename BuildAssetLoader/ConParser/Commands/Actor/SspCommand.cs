namespace BuildAssetLoader.Con
{
    // ssp <sprite1> <clipmask> — applies the sprite's own xvel/zvel via movesprite.
    public sealed record SspCommand(string Sprite1, string Clipmask) : Command(CommandList.ssp);
}

