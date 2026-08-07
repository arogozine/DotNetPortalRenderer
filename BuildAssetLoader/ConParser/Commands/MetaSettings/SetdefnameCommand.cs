namespace BuildAssetLoader.Con
{
    // setdefname <name>
    public sealed record SetdefnameCommand(string Name) : Command(CommandList.setdefname);
}

