// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Prepends additional code to the beginning of an existing, previously-declared state. This is the
    /// analog of <c>onevent</c> for states, and is mainly intended for modifying an existing state's code from a
    /// separate module.</summary>
    [Description("prependstate")]
    public sealed record PrependStateCommand(
        string Name) : BaseStateCommand(CommandList.PrependState, Name);
}
