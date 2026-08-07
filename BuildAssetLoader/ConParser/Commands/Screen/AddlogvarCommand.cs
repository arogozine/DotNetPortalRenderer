// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Alias-grammar sibling of <c>addlog</c>; prints the value of a gamevar or gamearray to the console
    /// and eduke32.log.</summary>
    [Description("addlogvar")]
    public sealed record AddLogVarCommand(
        string Gamevar) : Command(CommandList.AddLogVar);
}
