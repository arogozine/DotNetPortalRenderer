// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Assigns <c>Value</c> to a member of the player structure for the player given by <c>Id</c>.
    /// When <c>Id</c> is omitted it defaults to the current player.</summary>
    [Description("setplayer")]
    public sealed record SetPlayerCommand(
        string? Id,
        string Member,
        string Value) : Command(CommandList.SetPlayer);
}
