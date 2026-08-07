namespace BuildAssetLoader.Con
{
    public sealed record SetUserdefCommand(string Member, string Value) : Command(CommandList.setuserdef);
}

