// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Stops sound <c>Sound</c> playing from actor <c>SpriteId</c> specifically. Unlike <c>stopsound</c>,
    /// it only stops the sound coming from this one actor, leaving other instances of the same sound (e.g. from
    /// other actors) playing.</summary>
    [Description("stopactorsound")]
    public sealed record StopActorSoundCommand(
        string SpriteId,
        string Sound) : Command(CommandList.StopActorSound);
}
