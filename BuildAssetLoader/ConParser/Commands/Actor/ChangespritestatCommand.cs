namespace BuildAssetLoader.Con
{
    // changespritestat <sprite id> <statnum>
    public sealed record ChangespritestatCommand(string SpriteId, string Statnum) : Command(CommandList.changespritestat);
}

