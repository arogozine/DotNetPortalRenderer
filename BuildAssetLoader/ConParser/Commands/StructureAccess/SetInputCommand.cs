namespace BuildAssetLoader.Con
{
    public sealed record SetInputCommand(string? Id, string Member, string Value) : Command(CommandList.setinput);
}

