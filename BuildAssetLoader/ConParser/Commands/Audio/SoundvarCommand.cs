// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Gamevar-driven variant of <c>sound</c>: plays sound <c>SoundNumber</c> as defined by
    /// <c>definesound</c>, taking its sound value from a gamevar rather than a constant.</summary>
    [Description("soundvar")]
    public sealed record SoundVarCommand(
        string SoundNumber) : BaseSoundCommand(CommandList.SoundVar, SoundNumber);
}
