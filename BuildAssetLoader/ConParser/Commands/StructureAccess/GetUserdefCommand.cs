namespace BuildAssetLoader.Con
{
    // userdef has no id — the brackets are always empty ("userdef[].<member>").
    public sealed record GetUserdefCommand(string Member, string Gamevar) : Command(CommandList.getuserdef);
}

