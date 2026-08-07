namespace BuildAssetLoader.Con
{
    // updatesector <in_x> <in_y> <in_out_sectnum> — sector containing (x, y); in_out_sectnum must be pre-seeded.
    public sealed record UpdatesectorCommand(string InX, string InY, string InOutSectnum) : Command(CommandList.updatesector);
}

