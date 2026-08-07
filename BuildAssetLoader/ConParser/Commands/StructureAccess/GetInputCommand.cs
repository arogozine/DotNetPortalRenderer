namespace BuildAssetLoader.Con
{
    public sealed record GetInputCommand(string? Id, string Member, string Gamevar) : Command(CommandList.getinput);
}

