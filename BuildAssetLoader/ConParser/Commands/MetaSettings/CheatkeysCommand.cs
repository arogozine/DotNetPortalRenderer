// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Defines the two key-presses that must begin every cheat code, as scan codes. Defaults to
    /// 32 49 (D, N).</summary>
    [Description("cheatkeys")]
    public sealed record CheatKeysCommand(
        int ScanCode1,
        int ScanCode2) : Command(CommandList.CheatKeys);
}
