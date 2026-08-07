namespace BuildAssetLoader.Con
{
    // ===== Data Saving =====

    // readgamevar <varname> — restores a gamevar previously saved with savegamevar, from the user's config file.
    public sealed record ReadgamevarCommand(string VarName) : Command(CommandList.readgamevar);
}

