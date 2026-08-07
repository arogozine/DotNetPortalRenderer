// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Deprecated =====

    /// <summary>Obsolete command that stores a string value; never referenced by any current game logic.</summary>
    [Description("betaname")]
    public sealed record BetaNameCommand(
        string Value) : Command(CommandList.BetaName);
}
