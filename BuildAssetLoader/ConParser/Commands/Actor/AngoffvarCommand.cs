namespace BuildAssetLoader.Con
{
    // angoffvar <value> — sets the 3D model angle offset from a gamevar.
    public sealed record AngoffvarCommand(string Value) : Command(CommandList.angoffvar);
}

