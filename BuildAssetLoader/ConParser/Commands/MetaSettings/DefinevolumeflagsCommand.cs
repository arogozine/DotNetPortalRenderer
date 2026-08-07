namespace BuildAssetLoader.Con
{
    // definevolumeflags <vol> <flags>
    public sealed record DefinevolumeflagsCommand(int Volume, int Flags) : Command(CommandList.definevolumeflags);
}

