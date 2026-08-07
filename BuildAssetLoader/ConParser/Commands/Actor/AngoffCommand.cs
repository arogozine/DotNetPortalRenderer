namespace BuildAssetLoader.Con
{
    // angoff <value> — sets the 3D model angle offset from a constant.
    public sealed record AngoffCommand(string Value) : Command(CommandList.angoff);
}

