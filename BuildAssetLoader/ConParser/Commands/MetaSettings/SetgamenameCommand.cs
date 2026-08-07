namespace BuildAssetLoader.Con
{
    // setgamename <name>
    public sealed record SetgamenameCommand(string Name) : Command(CommandList.setgamename);
}

