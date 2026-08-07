// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Plays the sound <c>SoundNumber</c> as defined by <c>definesound</c>.</summary>
    [Description("sound")]
    public sealed record SoundCommand(
        string SoundNumber) : BaseSoundCommand(CommandList.Sound, SoundNumber);
}
