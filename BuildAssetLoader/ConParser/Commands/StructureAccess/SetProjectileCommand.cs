// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Assigns <c>Value</c> to a member of the projectile-type structure for the projectile identified
    /// by tile number <c>Id</c>. This addresses the shared per-tile projectile definition, not a specific
    /// in-flight projectile instance.</summary>
    [Description("setprojectile")]
    public sealed record SetProjectileCommand(
        string? Id,
        string Member,
        string Value) : Command(CommandList.SetProjectile);
}
