namespace BuildAssetLoader.Con
{
    // lotsofglass <number> — spawns broken glass pieces at the current actor.
    public sealed record LotsofglassCommand(string Number) : Command(CommandList.lotsofglass);
}

