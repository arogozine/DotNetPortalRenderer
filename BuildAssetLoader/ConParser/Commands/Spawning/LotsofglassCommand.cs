// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Causes the current actor to spawn the given number of broken glass pieces. If spritepal is used
    /// right before this command, it changes the glass palette.</summary>
    [Description("lotsofglass")]
    public sealed record LotsOfGlassCommand(
        string Number) : Command(CommandList.LotsOfGlass);
}
