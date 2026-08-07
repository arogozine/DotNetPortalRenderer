namespace BuildAssetLoader.Con
{
    // updatesectorz <in_x> <in_y> <in_z> <in_out_sectnum> — sector containing (x, y, z).
    public sealed record UpdatesectorzCommand(string InX, string InY, string InZ, string InOutSectnum) : Command(CommandList.updatesectorz);
}

