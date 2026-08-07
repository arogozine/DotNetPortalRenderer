// See: https://wiki.eduke32.com/wiki/Category:All_commands

namespace BuildAssetLoader.Con
{
    /// <summary>Shared base for the <c>endofgame</c>/<c>endoflevel</c> commands (the latter being the original
    /// pre-v1.1 name, kept for compatibility). Triggers the end of the episode after <c>Number</c> 1/15-second
    /// time units (default 52). <c>Start</c> selects which of the two token spellings was used.</summary>
    public record BaseEndOfGameCommand(CommandList Start, int Number) : Command(Start);
}
