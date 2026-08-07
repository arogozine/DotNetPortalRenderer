// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Enforces a fixed compiled-code buffer size, bypassing the normal progressive buffer growth
    /// (16384 -> 32768 -> 65536, doubling each step) that happens during startup.</summary>
    [Description("scriptsize")]
    public sealed record ScriptSizeCommand(
        int Size) : Command(CommandList.ScriptSize);
}
