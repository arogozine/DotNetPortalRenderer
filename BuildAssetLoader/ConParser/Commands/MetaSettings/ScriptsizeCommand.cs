namespace BuildAssetLoader.Con
{
    // scriptsize <integer>
    public sealed record ScriptsizeCommand(int Size) : Command(CommandList.scriptsize);
}

