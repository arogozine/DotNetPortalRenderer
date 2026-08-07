// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Copies a substring of quote <c>Quote2</c>, beginning at position <c>Start</c> and continuing for
    /// <c>Length</c> characters, into quote <c>Quote1</c>.</summary>
    [Description("qsubstr")]
    public sealed record QSubStrCommand(
        int Quote1,
        int Quote2,
        int Start,
        int Length) : Command(CommandList.QSubStr);
}
