// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Adds to the closest player's kill score, and also clears the current actor's stayput flag
    /// (most likely to let corpses fall off ledges in the vanilla game).</summary>
    [Description("addkills")]
    public sealed record AddKillsCommand(
        string Number) : Command(CommandList.AddKills);
}
