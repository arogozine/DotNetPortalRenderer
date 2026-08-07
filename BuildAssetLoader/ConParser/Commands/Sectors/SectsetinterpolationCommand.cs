namespace BuildAssetLoader.Con
{
    // sectsetinterpolation <sectnum> — smooths a CON-driven sector's movement.
    public sealed record SectsetinterpolationCommand(string Sectnum) : Command(CommandList.sectsetinterpolation);
}

