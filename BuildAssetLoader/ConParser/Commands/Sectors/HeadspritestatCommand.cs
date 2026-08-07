namespace BuildAssetLoader.Con
{
    // headspritestat <sprite> <statnum> — first sprite id in a statnum's linked list.
    public sealed record HeadspritestatCommand(string Sprite, string Statnum) : Command(CommandList.headspritestat);
}

