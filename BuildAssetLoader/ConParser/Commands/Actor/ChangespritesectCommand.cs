namespace BuildAssetLoader.Con
{
    // changespritesect <actorid> <sectnum>
    public sealed record ChangespritesectCommand(string ActorId, string Sectnum) : Command(CommandList.changespritesect);
}

