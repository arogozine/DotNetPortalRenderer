// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Reads a member of the projectile-type structure for the projectile identified by tile number
    /// <c>Id</c> into <c>Gamevar</c>. This addresses the shared per-tile projectile definition, not a specific
    /// in-flight projectile instance.</summary>
    [Description("getprojectile")]
    public sealed record GetProjectileCommand(
        string? Id,
        string Member,
        string Gamevar) : Command(CommandList.GetProjectile);
}
