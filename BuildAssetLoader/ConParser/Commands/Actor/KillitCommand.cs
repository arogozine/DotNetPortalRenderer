namespace BuildAssetLoader.Con
{
    // killit — deletes the current actor from the map; halts further execution like return.
    public sealed record KillitCommand() : Command(CommandList.killit);
}

