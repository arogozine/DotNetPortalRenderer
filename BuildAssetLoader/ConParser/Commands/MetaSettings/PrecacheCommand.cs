namespace BuildAssetLoader.Con
{
    // precache <tile0> <tile1> <flag>
    public sealed record PrecacheCommand(int Tile0, int Tile1, int Flag) : Command(CommandList.precache);
}

