// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Original pre-v1.1 name for <c>endofgame</c>, kept for compatibility. Triggers the end of the
    /// episode after <c>Number</c> 1/15-second time units (default 52).</summary>
    [Description("endoflevel")]
    public sealed record EndOfLevelCommand(
        int Number) : BaseEndOfGameCommand(CommandList.EndOfLevel, Number);
}
