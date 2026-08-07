// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Assigns <c>Value</c> to a member of the input structure for the player given by <c>Id</c>.
    /// Generally used within EVENT_PROCESSINPUT. When <c>Id</c> is omitted it defaults to the current player.</summary>
    [Description("setinput")]
    public sealed record SetInputCommand(
        string? Id,
        string Member,
        string Value) : Command(CommandList.SetInput);
}
