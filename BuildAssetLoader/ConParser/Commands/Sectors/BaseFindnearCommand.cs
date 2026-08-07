namespace BuildAssetLoader.Con
{
    // ===== Discovery - Searching =====

    // findnearactor/findnearactorvar/findnearactor3d/findnearactor3dvar/findnearsprite/findnearspritevar/findnearsprite3d/findnearsprite3dvar
    // <tile number> <distance> <gamevar> — cylinder (normal) or sphere (3d) search radius.
    public record BaseFindnearCommand(CommandList Start, string TileNumber, string Distance, string Gamevar) : Command(Start);
}

