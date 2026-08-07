// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Sets the global maximum amount of ammo for a weapon; the change is reflected in the player's
    /// HUD.</summary>
    [Description("smaxammo")]
    public sealed record SMaxAmmoCommand(
        string WeaponId,
        string MaxAmount) : Command(CommandList.SMaxAmmo);
}
