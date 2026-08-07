// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Appends the first <c>Num</c> characters of quote <c>Quote2</c> to the end of quote
    /// <c>Quote1</c>.</summary>
    [Description("qstrncat")]
    public sealed record QStrNCatCommand(
        int Quote1,
        int Quote2,
        int Num) : Command(CommandList.QStrNCat);
}
