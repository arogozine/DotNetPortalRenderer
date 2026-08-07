// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Dummy command in EDuke32 — compiles but does nothing.</summary>
    [Description("shadeto")]
    public sealed record ShadeToCommand(
        string Value) : Command(CommandList.ShadeTo);
}
