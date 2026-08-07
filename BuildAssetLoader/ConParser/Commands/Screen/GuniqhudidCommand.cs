// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Selects the animation-state slot (0-254, via <c>SlotId</c>) used for HUD models drawn with
    /// <c>rotatesprite</c>. Since HUD-drawn models have no sprite id of their own, this lets several animating HUD
    /// models keep independent state; switch back to slot 0 when done.</summary>
    [Description("guniqhudid")]
    public sealed record GUniqHudIdCommand(
        string SlotId) : Command(CommandList.GUniqHudId);
}
