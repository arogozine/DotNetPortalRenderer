// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Gamevar-driven variant of <c>stopsound</c>: stops the playing of sound <c>SoundNumber</c>, taking
    /// its sound value from a gamevar rather than a constant.</summary>
    [Description("stopsoundvar")]
    public sealed record StopSoundVarCommand(
        string SoundNumber) : BaseSoundCommand(CommandList.StopSoundVar, SoundNumber);
}
