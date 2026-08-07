// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Changes the number of elements held by the previously-defined gamearray <c>ArrayName</c> to
    /// <c>NewSize</c>.</summary>
    [Description("resizearray")]
    public sealed record ResizeArrayCommand(
        string ArrayName,
        string NewSize) : Command(CommandList.ResizeArray);
}
