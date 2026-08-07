namespace BuildAssetLoader.Con
{
    // pstomp — makes the nearest player look down and stomp; place in the code of the actor being stomped.
    public sealed record PstompCommand() : Command(CommandList.pstomp);
}

