namespace BuildAssetLoader.Con
{
    // savegamevar <varname> — persists a gamevar to the user's config file.
    public sealed record SavegamevarCommand(string VarName) : Command(CommandList.savegamevar);
}

