namespace BuildAssetLoader.Con
{
    // mail <number> — spawns envelopes at the current actor.
    public sealed record MailCommand(string Number) : Command(CommandList.mail);
}

