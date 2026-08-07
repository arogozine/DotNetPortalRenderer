// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Appends the text of quote <c>Quote2</c> to the end of quote <c>Quote1</c>, with no separator
    /// inserted between them.</summary>
    [Description("qstrcat")]
    public sealed record QStrCatCommand(
        int Quote1,
        int Quote2) : Command(CommandList.QStrCat);
}
