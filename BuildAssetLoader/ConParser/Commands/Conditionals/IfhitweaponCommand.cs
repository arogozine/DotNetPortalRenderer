// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Checks if the current actor was struck by a weapon. Built-in damage processing occurs when this is
    /// evaluated, so it must be called frequently in actor code for the actor to be affected by projectiles. See
    /// also <c>ifwasweapon</c>.</summary>
    [Description("ifhitweapon")]
    public sealed record IfHitWeaponCommand() : ConditionalStructure(CommandList.IfHitWeapon);
}
