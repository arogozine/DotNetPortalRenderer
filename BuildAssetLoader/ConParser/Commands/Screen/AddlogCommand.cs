// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Prints the value of a gamevar or gamearray to the console, which is also stored in eduke32.log.
    /// <c>Gamevar</c> is the variable/array (or, on newer builds, a constant/actor var/player var/struct member)
    /// to log.</summary>
    [Description("addlog")]
    public sealed record AddLogCommand(
        string Gamevar) : Command(CommandList.AddLog);
}
