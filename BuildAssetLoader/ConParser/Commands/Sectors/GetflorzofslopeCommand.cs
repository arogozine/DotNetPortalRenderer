namespace BuildAssetLoader.Con
{
    // getflorzofslope <sectnum> <x> <y> <return>
    public sealed record GetflorzofslopeCommand(string Sectnum, string X, string Y, string ReturnVar) : Command(CommandList.getflorzofslope);
}

