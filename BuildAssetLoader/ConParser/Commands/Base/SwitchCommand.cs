// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>C-like <c>switch &lt;gamevar&gt; case &lt;c&gt; {...} break ... default {...} break endswitch</c>.
    /// Each case works like an <c>ifvare</c> comparison against <c>Gamevar</c>; <c>default</c> matches every value
    /// not covered by a case. Every case/default block must be terminated with a <c>break</c>.</summary>
    [Description("switch")]
    public sealed record SwitchCommand(
        string Gamevar,
        List<CaseBlock> Cases) : Structure(CommandList.Switch, CommandList.EndSwitch);
}
