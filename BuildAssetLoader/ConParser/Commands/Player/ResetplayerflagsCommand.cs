namespace BuildAssetLoader.Con
{
    // resetplayerflags <flags> — same as resetplayer, with a bitfield of extra options.
    public sealed record ResetplayerflagsCommand(int Flags) : Command(CommandList.resetplayerflags);
}

