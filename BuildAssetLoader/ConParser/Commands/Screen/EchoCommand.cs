namespace BuildAssetLoader.Con
{
    // echo <quote number> — prints a quote to the console/log only, not to the screen.
    public sealed record EchoCommand(int QuoteNumber) : Command(CommandList.echo);
}

