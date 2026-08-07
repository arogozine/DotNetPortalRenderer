namespace BuildAssetLoader.Con
{
    // ===== Preprocessor =====

    // define <name> <value> — <value> is usually an int literal, but may also be another define's name
    // or a symbolic constant (e.g. YES/NO), so it isn't resolved/parsed as an int here.
    public record DefineCommand(string Name, string Value) : Command(CommandList.define);
}

