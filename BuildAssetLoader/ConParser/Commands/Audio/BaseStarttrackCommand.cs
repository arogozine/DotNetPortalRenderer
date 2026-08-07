// See: https://wiki.eduke32.com/wiki/Category:All_commands

namespace BuildAssetLoader.Con
{
    /// <summary>Shared base for the <c>starttrack</c>/<c>starttrackvar</c> commands, which change the currently
    /// playing background music track for the current episode as defined by <c>music</c>.</summary>
    public record BaseStarttrackCommand(CommandList Start, string Track) : Command(Start);
}
