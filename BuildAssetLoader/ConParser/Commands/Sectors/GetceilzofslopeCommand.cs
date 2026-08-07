namespace BuildAssetLoader.Con
{
    // ===== Sectors - Analysis =====

    // getceilzofslope <sectnum> <x> <y> <return>
    public sealed record GetceilzofslopeCommand(string Sectnum, string X, string Y, string ReturnVar) : Command(CommandList.getceilzofslope);
}

