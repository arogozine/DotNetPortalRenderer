namespace BuildAssetLoader.Con
{
    // definegametype <gametypenum> <flags> <name>
    public sealed record DefinegametypeCommand(int GameTypeNum, int Flags, string Name) : Command(CommandList.definegametype);
}

