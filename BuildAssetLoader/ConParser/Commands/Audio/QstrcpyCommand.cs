// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Copies the text of quote <c>Quote2</c> into quote <c>Quote1</c>, overwriting its previous
    /// contents.</summary>
    [Description("qstrcpy")]
    public sealed record QStrCpyCommand(
        int Quote1,
        int Quote2) : Command(CommandList.QStrCpy);
}
