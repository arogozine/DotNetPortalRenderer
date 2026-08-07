namespace BuildAssetLoader.Con
{
    // qstrdim <width return var> <height return var> <tilenum> <x> <y> <zoom> <block angle> <quote> <orientation>
    //         <xspace> <yline> <xbetween> <ybetween> <text flags> <x1> <y1> <x2> <y2>
    // Calculates the on-screen dimensions of an equivalent screentext call.
    public sealed record QstrdimCommand(
        string WidthReturnVar, string HeightReturnVar, string TileNum, string X, string Y, string Zoom, string BlockAngle,
        int Quote, string Orientation, string Xspace, string Yline, string Xbetween, string Ybetween, string TextFlags,
        string X1, string Y1, string X2, string Y2)
        : Command(CommandList.qstrdim);
}

