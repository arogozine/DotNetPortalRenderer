// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Assigns <c>Value</c> to a per-player custom gamevar for the player given by <c>Id</c>. When
    /// <c>Id</c> is omitted it defaults to the current player.</summary>
    [Description("setplayervar")]
    public sealed record SetPlayerVarCommand(
        string? Id,
        string Member,
        string Value) : Command(CommandList.SetPlayerVar);
}
