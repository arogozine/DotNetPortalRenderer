// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Shared base for <c>addweapon</c>/<c>addweaponvar</c>: gives a weapon with the specified amount of
    /// ammo to the nearest player. Synthetic base class used to share logic between the two CON commands in this
    /// codebase; not itself a CON keyword.</summary>
    public record BaseAddWeaponCommand(CommandList Start, string Weapon, string Amount) : Command(Start);
}
