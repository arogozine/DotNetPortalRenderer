// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Reads a member of the player structure for the player given by <c>Id</c> into <c>Gamevar</c>.
    /// When <c>Id</c> is omitted it defaults to the current player.</summary>
    [Description("getplayer")]
    public sealed record GetPlayerCommand(
        string? Id,
        string Member,
        string Gamevar) : Command(CommandList.GetPlayer);
}
