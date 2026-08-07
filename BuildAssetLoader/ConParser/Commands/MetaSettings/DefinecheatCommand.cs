// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Rebinds the activation text for one of the 27 hard-coded cheat codes (their functionality
    /// cannot be changed, only the phrase used to trigger it). See <c>cheatkeys</c> for the two key-presses
    /// that must precede any cheat phrase.</summary>
    [Description("definecheat")]
    public sealed record DefineCheatCommand(
        int CheatNumber,
        string ActivationText) : Command(CommandList.DefineCheat);
}
