namespace BuildAssetLoader.Con
{
    // rayintersect <x> <y> <z> <vx> <vy> <vz> <x1> <y1> <x2> <y2> <intx> <inty> <intz> <ret>
    public sealed record RayintersectCommand(
        string X, string Y, string Z, string Vx, string Vy, string Vz,
        string X1, string Y1, string X2, string Y2, string Intx, string Inty, string Intz, string Ret)
        : Command(CommandList.rayintersect);
}

