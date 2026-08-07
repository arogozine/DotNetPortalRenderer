namespace BuildAssetLoader.Con
{
    // shiftvarr <gamevar> <number> — right-shift <gamevar> by <number> bits.
    public sealed record ShiftvarrCommand(string Gamevar, string Number) : Command(CommandList.shiftvarr);
}

