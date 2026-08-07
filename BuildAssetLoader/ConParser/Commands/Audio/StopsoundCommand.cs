// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Stops the playing of sound <c>SoundNumber</c>. See also <c>stopallsounds</c>.</summary>
    [Description("stopsound")]
    public sealed record StopSoundCommand(
        string SoundNumber) : BaseSoundCommand(CommandList.StopSound, SoundNumber);
}
