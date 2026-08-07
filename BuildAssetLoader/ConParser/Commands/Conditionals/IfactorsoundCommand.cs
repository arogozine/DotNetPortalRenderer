// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Detects whether the sprite identified by SpriteId is currently playing the given sound number.</summary>
    [Description("ifactorsound")]
    public sealed record IfActorSoundCommand(
        string SpriteId,
        string Sound) : ConditionalStructure(CommandList.IfActorSound);
}
