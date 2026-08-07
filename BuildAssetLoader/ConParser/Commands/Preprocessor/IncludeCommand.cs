// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Inserts the contents of the specified file as if it were part of the current file at this point.</summary>
    [Description("include")]
    public sealed record IncludeCommand(
        string Filename) : Command(CommandList.Include);
}
