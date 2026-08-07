// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Variable-input form of <c>addweapon</c>: gives the specified weapon with the specified amount of
    /// ammo to the nearest player, taking gamevars rather than constants/defines for its inputs. Same full-ammo
    /// halting behavior as <c>addweapon</c>.</summary>
    [Description("addweaponvar")]
    public sealed record AddWeaponVarCommand(
        string Weapon,
        string Amount) : BaseAddWeaponCommand(CommandList.AddWeaponVar, Weapon, Amount);
}
