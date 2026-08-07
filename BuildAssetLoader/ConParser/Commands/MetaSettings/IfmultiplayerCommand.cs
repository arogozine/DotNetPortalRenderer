// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Conditional structure that evaluates true if the game is currently running in multiplayer.</summary>
    [Description("ifmultiplayer")]
    public sealed record IfMultiplayerCommand() : ConditionalStructure(CommandList.IfMultiplayer);
}
