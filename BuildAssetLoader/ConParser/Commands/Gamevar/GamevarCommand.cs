namespace BuildAssetLoader.Con
{
    // ===== Game Variables =====

    // gamevar <varname> <value> <flags> — value/flags may be omitted (defaults to a global var initialized to 0).
    public sealed record GamevarCommand(string Name, int? Value, int? Flags) : Command(CommandList.gamevar);
}

