namespace BuildAssetLoader.Con
{
    // movesprite <sprite id> <xvel> <yvel> <zvel> <clipmask> <returnvar>
    public sealed record MovespriteCommand(string SpriteId, string Xvel, string Yvel, string Zvel, string Clipmask, string ReturnVar)
        : Command(CommandList.movesprite);
}

