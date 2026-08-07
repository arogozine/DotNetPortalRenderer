// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Conditional structure that evaluates true if the executing game instance is the server, as
    /// opposed to a client. See also <c>ifmultiplayer</c> and <c>ifclient</c>.</summary>
    [Description("ifserver")]
    public sealed record IfServerCommand() : ConditionalStructure(CommandList.IfServer);
}
