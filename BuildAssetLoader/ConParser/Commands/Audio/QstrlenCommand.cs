// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Measures the length of quote <c>Quote</c> and records it into <c>Gamevar</c>.</summary>
    [Description("qstrlen")]
    public sealed record QStrLenCommand(
        string Gamevar,
        int Quote) : Command(CommandList.QStrLen);
}
