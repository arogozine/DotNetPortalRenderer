// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Conditional returning true if a projectile is near the actor. For hitscan projectiles (e.g.
    /// SHOTSPARK1), returns true if the point of impact is near the player.</summary>
    [Description("ifbulletnear")]
    public sealed record IfBulletNearCommand() : ConditionalStructure(CommandList.IfBulletNear);
}
