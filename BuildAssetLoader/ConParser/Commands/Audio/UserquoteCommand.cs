namespace BuildAssetLoader.Con
{
    // userquote <quote number> — adds a quote to the four-line multiplayer/chat text buffer.
    public sealed record UserquoteCommand(int QuoteNumber) : Command(CommandList.userquote);
}

