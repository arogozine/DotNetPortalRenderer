namespace BuildAssetLoader.Con
{
    // writearraytofile <array name> <quote number> — quote holds the file name.
    public sealed record WritearraytofileCommand(string ArrayName, int QuoteNumber) : Command(CommandList.writearraytofile);
}

