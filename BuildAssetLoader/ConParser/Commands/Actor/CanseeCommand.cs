namespace BuildAssetLoader.Con
{
    // cansee <x1> <y1> <z1> <sect1> <x2> <y2> <z2> <sect2> <returnvar>
    public sealed record CanseeCommand(
        string X1, string Y1, string Z1, string Sect1,
        string X2, string Y2, string Z2, string Sect2, string ReturnVar)
        : Command(CommandList.cansee);
}

