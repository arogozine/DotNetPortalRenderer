// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Math Operations =====

    /// <summary>Computes the square root of <c>Input</c> and stores the result in <c>Output</c>.</summary>
    [Description("sqrt")]
    public sealed record SqrtCommand(
        string Input,
        string Output) : Command(CommandList.Sqrt);
}
