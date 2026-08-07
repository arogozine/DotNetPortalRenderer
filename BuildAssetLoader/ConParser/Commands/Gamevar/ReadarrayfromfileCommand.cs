namespace BuildAssetLoader.Con
{
    // readarrayfromfile <array name> <quote number> — quote holds the file name; resizes the array to fit.
    public sealed record ReadarrayfromfileCommand(string ArrayName, int QuoteNumber) : Command(CommandList.readarrayfromfile);
}

