namespace BuildAssetLoader.Con
{
    // setactorsoundpitch <actor#> <sound#> <pitchoffset>
    public sealed record SetactorsoundpitchCommand(string ActorId, string Sound, string PitchOffset) : Command(CommandList.setactorsoundpitch);
}

