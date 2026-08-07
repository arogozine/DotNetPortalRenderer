// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Appends additional code to the end of an existing, previously-declared state. This is the analog
    /// of <c>appendevent</c> for states, and is mainly intended for modifying an existing state's code from a
    /// separate module.</summary>
    [Description("appendstate")]
    public sealed record AppendStateCommand(
        string Name) : BaseStateCommand(CommandList.AppendState, Name);
}
