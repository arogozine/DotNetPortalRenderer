// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Cutscenes =====

    /// <summary>Starts the cutscene whose path is stored in the quote identified by <c>QuoteId</c>.</summary>
    [Description("startcutscene")]
    public sealed record StartCutsceneCommand(
        int QuoteId) : Command(CommandList.StartCutscene);
}
