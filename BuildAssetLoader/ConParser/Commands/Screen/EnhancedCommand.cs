namespace BuildAssetLoader.Con
{
    // enhanced <value> — obsolete CON version compatibility marker.
    public sealed record EnhancedCommand(int Value) : Command(CommandList.enhanced);
}

