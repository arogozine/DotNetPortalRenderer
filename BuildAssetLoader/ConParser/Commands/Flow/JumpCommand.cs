namespace BuildAssetLoader.Con
{
    // jump <address> — <address> is a gamevar previously populated by getcurraddress.
    public sealed record JumpCommand(string Address) : Command(CommandList.jump);
}

