namespace BuildAssetLoader.Con
{
    // shiftvarl <gamevar> <number> — left-shift <gamevar> by <number> bits.
    public sealed record ShiftvarlCommand(string Gamevar, string Number) : Command(CommandList.shiftvarl);
}

