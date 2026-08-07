// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Conditional structure that evaluates true if the executing game instance is a client, as
    /// opposed to the server. See also <c>ifmultiplayer</c> and <c>ifserver</c>.</summary>
    [Description("ifclient")]
    public sealed record IfClientCommand() : ConditionalStructure(CommandList.IfClient);
}
