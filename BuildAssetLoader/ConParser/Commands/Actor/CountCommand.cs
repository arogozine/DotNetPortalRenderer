// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Actors - Structures (remainder) =====

    /// <summary>Sets the current actor's count to <c>Number</c>. Count is incremented by 1 every actor code cycle;
    /// use <c>ifcount</c> to test it and <c>resetcount</c> to reset it to 0.</summary>
    [Description("count")]
    public sealed record CountCommand(string Number) : Command(CommandList.Count);
}
