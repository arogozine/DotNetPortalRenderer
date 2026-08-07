// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Deprecated command. Sets the current player's angle from <c>Gamevar</c>. Superseded by struct
    /// access (e.g. <c>setplayer[].ang</c>).</summary>
    [Description("setplayerangle")]
    public sealed record SetPlayerAngleCommand(
        string Gamevar) : Command(CommandList.SetPlayerAngle);
}
