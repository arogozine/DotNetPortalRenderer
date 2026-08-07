// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Executes, from within actor code, the previously-declared "ai" routine named <c>Name</c>.</summary>
    [Description("ai")]
    public sealed record AiInvokeCommand(string Name) : Command(CommandList.Ai);
}
