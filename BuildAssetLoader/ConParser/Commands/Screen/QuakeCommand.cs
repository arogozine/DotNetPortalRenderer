namespace BuildAssetLoader.Con
{
    // quake <count> — shakes the screen for <count> tics (26 = 1 second); also triggers lotag-33 sector effectors.
    public sealed record QuakeCommand(string Count) : Command(CommandList.quake);
}

