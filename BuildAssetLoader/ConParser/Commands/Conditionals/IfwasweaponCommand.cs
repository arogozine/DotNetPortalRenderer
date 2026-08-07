// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Companion to <c>ifhitweapon</c>. Checks if the current actor was struck by the specific
    /// Weapon.</summary>
    [Description("ifwasweapon")]
    public sealed record IfWasWeaponCommand(
        string Weapon) : ConditionalStructure(CommandList.IfWasWeapon);
}
