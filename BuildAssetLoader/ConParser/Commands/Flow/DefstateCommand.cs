// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Declares a named, reusable block of CON code outside of any actor or event, so it can be invoked
    /// from multiple places without duplicating code. Functionally identical to <c>state</c>, but preferred when
    /// using an editor/highlighter that can collapse code blocks.</summary>
    [Description("defstate")]
    public sealed record DefStateCommand(
        string Name) : BaseStateCommand(CommandList.DefState, Name);
}
