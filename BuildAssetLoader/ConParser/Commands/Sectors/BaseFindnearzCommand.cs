namespace BuildAssetLoader.Con
{
    // findnearactorz/findnearactorzvar/findnearspritez/findnearspritezvar
    // <tile number> <xydistance> <zdistance> <gamevar> — finite cylinder search.
    public record BaseFindnearzCommand(CommandList Start, string TileNumber, string XyDistance, string ZDistance, string Gamevar) : Command(Start);
}

