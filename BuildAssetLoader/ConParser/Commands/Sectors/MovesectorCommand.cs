namespace BuildAssetLoader.Con
{
    // movesector <sprite ID> — moves a sector in x/y using the sprite's xvel/yvel (earthquakes, rotators, doors, escalators).
    public sealed record MovesectorCommand(string SpriteId) : Command(CommandList.movesector);
}

