namespace BuildAssetLoader.Con
{
    // getlastpal — restores the actor's palette to what it was prior to the last spritepal.
    public sealed record GetlastpalCommand() : Command(CommandList.getlastpal);
}

