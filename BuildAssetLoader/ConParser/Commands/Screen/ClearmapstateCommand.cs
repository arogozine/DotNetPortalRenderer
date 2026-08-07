namespace BuildAssetLoader.Con
{
    // clearmapstate <level> — clears a specific map from the map cache (VOLUME*MAXLEVELS+LEVEL).
    public sealed record ClearmapstateCommand(string Level) : Command(CommandList.clearmapstate);
}

