// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Conditional returning true if the current actor is not marked as stayput.</summary>
    [Description("ifactornotstayput")]
    public sealed record IfActorNotStayPutCommand() : ConditionalStructure(CommandList.IfActorNotStayPut);
}
