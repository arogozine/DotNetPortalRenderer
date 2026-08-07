// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Branches based on whether the cutscene identified by the quote QuoteId (a quote holding the
    /// cutscene path) is currently playing. Only meaningful inside EVENT_CUTSCENE, EVENT_PRECUTSCENE and
    /// EVENT_SKIPCUTSCENE; always false elsewhere.</summary>
    [Description("ifcutscene")]
    public sealed record IfCutsceneCommand(
        int QuoteId) : ConditionalStructure(CommandList.IfCutscene);
}
