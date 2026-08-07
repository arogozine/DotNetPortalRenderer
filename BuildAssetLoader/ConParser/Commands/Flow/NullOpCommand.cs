// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Flow Control - If Components =====

    /// <summary>No-op placeholder, used in place of empty braces — typically as the true-branch of an
    /// if-statement that only needs an else-branch (e.g. <c>ifpdistl 1024 nullop else killit</c>).</summary>
    [Description("nullop")]
    public sealed record NullOpCommand() : Command(CommandList.NullOp);
}
