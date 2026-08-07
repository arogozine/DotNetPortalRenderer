namespace BuildAssetLoader.Con
{
    // clipmovenoslide <return> <x> <y> <z> <sectnum> <xvect> <yvect> <walldist> <flordist> <ceildist> <clipmask>
    public sealed record ClipmovenoslideCommand(
        string Return, string X, string Y, string Z, string Sectnum,
        string Xvect, string Yvect, string Walldist, string Flordist, string Ceildist, string Clipmask)
        : Command(CommandList.clipmovenoslide);
}

