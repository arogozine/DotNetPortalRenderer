// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Gets the global maximum amount of ammo for a weapon and stores it into a return gamevar.</summary>
    [Description("gmaxammo")]
    public sealed record GMaxAmmoCommand(
        string WeaponId,
        string ReturnVar) : Command(CommandList.GMaxAmmo);
}
