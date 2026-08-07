// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Plays the sound <c>SoundNumber</c> as defined by <c>definesound</c>, like <c>sound</c>, except the
    /// same sound cannot begin playing again until any currently playing instance of it (from any actor) has
    /// finished.</summary>
    [Description("soundonce")]
    public sealed record SoundOnceCommand(
        string SoundNumber) : BaseSoundCommand(CommandList.SoundOnce, SoundNumber);
}
