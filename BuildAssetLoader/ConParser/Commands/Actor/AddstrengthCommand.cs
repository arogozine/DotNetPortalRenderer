namespace BuildAssetLoader.Con
{
    // addstrength <number> — adjusts the actor's health by a signed delta; commonly a define.
    public sealed record AddstrengthCommand(string Number) : Command(CommandList.addstrength);
}

