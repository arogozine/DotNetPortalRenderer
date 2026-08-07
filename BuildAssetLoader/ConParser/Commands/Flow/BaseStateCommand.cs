namespace BuildAssetLoader.Con
{
    // ===== Global Settings - Subroutines (states) =====

    // state <name> ... ends / defstate <name> ... ends / prependstate <name> ... ends / appendstate <name> ... ends
    public record BaseStateCommand(CommandList Start, string Name) : Structure(Start, CommandList.ends);
}

