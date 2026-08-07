namespace BuildAssetLoader.Con
{
    // startlevel <volume> <level> — schedules a map load, bypassing the End of Level screen.
    public sealed record StartlevelCommand(string Volume, string Level) : Command(CommandList.startlevel);
}

