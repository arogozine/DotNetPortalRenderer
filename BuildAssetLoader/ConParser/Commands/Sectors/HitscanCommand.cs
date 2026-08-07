namespace BuildAssetLoader.Con
{
    // hitscan <x1> <y1> <z1> <sect1> <cos of ang> <sin of ang> <zvel> <hit sector> <hit wall> <hit sprite> <hit x> <hit y> <hit z> <clip mask>
    public sealed record HitscanCommand(
        string X1, string Y1, string Z1, string Sect1, string CosAng, string SinAng, string Zvel,
        string HitSectorVar, string HitWallVar, string HitSpriteVar, string HitXVar, string HitYVar, string HitZVar, string ClipMask)
        : Command(CommandList.hitscan);
}

