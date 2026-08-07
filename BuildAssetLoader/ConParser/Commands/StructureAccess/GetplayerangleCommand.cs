// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Deprecated command. Gets the current player's angle into <c>Gamevar</c>. Superseded by struct
    /// access (e.g. <c>getplayer[].ang</c>).</summary>
    [Description("getplayerangle")]
    public sealed record GetPlayerAngleCommand(
        string Gamevar) : Command(CommandList.GetPlayerAngle);
}
