// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Reads a member of the input structure for the player given by <c>Id</c> into <c>Gamevar</c>.
    /// Generally used within EVENT_PROCESSINPUT. When <c>Id</c> is omitted it defaults to the current player.</summary>
    [Description("getinput")]
    public sealed record GetInputCommand(
        string? Id,
        string Member,
        string Gamevar) : Command(CommandList.GetInput);
}
