namespace BuildAssetLoader.Con
{
    // showviewunbiased <x> <y> <z> <angle> <horiz> <sector> <scrn_x1> <scrn_y1> <scrn_x2> <scrn_y2>
    public sealed record ShowviewunbiasedCommand(
        string X, string Y, string Z, string Angle, string Horiz, string Sector,
        string ScrnX1, string ScrnY1, string ScrnX2, string ScrnY2)
        : Command(CommandList.showviewunbiased);
}

