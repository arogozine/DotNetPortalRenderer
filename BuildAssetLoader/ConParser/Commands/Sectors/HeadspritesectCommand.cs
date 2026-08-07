namespace BuildAssetLoader.Con
{
    // ===== Sorting =====

    // headspritesect <sprite> <sect> — first sprite id in a sector's linked list.
    public sealed record HeadspritesectCommand(string Sprite, string Sect) : Command(CommandList.headspritesect);
}

