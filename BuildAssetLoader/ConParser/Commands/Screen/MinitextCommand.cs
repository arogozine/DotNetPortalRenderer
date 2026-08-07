namespace BuildAssetLoader.Con
{
    // minitext <x> <y> <quote> <shade> <pal>
    public sealed record MinitextCommand(string X, string Y, int Quote, string Shade, string Pal) : Command(CommandList.minitext);
}

