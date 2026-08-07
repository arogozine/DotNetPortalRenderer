namespace BuildAssetLoader.Con
{
    // lineintersect <ox> <oy> <oz> <dx> <dy> <dz> <x1> <y1> <x2> <y2> <intx> <inty> <intz> <ret>
    public sealed record LineintersectCommand(
        string Ox, string Oy, string Oz, string Dx, string Dy, string Dz,
        string X1, string Y1, string X2, string Y2, string Intx, string Inty, string Intz, string Ret)
        : Command(CommandList.lineintersect);
}

