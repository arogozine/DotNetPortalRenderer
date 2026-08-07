namespace BuildAssetLoader.Con
{
    // screentext <tilenum> <x> <y> <zoom> <block angle> <character angle> <quote> <shade> <pal> <orientation>
    //            <alpha> <xspace> <yline> <xbetween> <ybetween> <text flags> <x1> <y1> <x2> <y2>
    public sealed record ScreentextCommand(
        string TileNum, string X, string Y, string Zoom, string BlockAngle, string CharacterAngle, int Quote,
        string Shade, string Pal, string Orientation, string Alpha, string Xspace, string Yline, string Xbetween,
        string Ybetween, string TextFlags, string X1, string Y1, string X2, string Y2)
        : Command(CommandList.screentext);
}

