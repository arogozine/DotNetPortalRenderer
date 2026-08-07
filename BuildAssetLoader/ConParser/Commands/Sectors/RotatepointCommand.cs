namespace BuildAssetLoader.Con
{
    // rotatepoint <xpivot> <ypivot> <x> <y> <ang> <xreturnvar> <yreturnvar>
    public sealed record RotatepointCommand(
        string Xpivot, string Ypivot, string X, string Y, string Ang, string XReturnVar, string YReturnVar)
        : Command(CommandList.rotatepoint);
}

