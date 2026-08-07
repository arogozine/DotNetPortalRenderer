// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Declares a named, reusable block of CON code outside of any actor or event, so it can be invoked
    /// from multiple places without duplicating code. <c>defstate</c> is preferred for this over <c>state</c> when
    /// using an editor/highlighter that can collapse blocks, but both behave identically as declarations.</summary>
    [Description("state")]
    public sealed record StateCommand(
        string Name) : BaseStateCommand(CommandList.State, Name);
}
