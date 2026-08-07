// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Array Operations =====

    /// <summary>Returns the number of elements the gamearray named by <c>ArrayName</c> currently holds, storing the
    /// count in <c>ReturnVar</c>.</summary>
    [Description("getarraysize")]
    public sealed record GetArraySizeCommand(
        string ArrayName,
        string ReturnVar) : Command(CommandList.GetArraySize);
}
