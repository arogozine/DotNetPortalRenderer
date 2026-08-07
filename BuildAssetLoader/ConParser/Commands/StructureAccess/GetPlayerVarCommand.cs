// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Reads a per-player custom gamevar for the player given by <c>Id</c> into <c>Gamevar</c>.
    /// When <c>Id</c> is omitted it defaults to the current player.</summary>
    [Description("getplayervar")]
    public sealed record GetPlayerVarCommand(
        string? Id,
        string Member,
        string Gamevar) : Command(CommandList.GetPlayerVar);
}
