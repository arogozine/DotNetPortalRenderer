namespace BuildAssetLoader.Con
{
    // sectclearinterpolation <sectnum> — reverts a sector to the native 30Hz update rate.
    public sealed record SectclearinterpolationCommand(string Sectnum) : Command(CommandList.sectclearinterpolation);
}

