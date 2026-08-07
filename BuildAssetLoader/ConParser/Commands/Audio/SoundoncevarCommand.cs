// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Gamevar-driven variant of <c>soundonce</c>: plays sound <c>SoundNumber</c> unless an instance of it
    /// is already playing, taking its sound value from a gamevar rather than a constant.</summary>
    [Description("soundoncevar")]
    public sealed record SoundOnceVarCommand(
        string SoundNumber) : BaseSoundCommand(CommandList.SoundOnceVar, SoundNumber);
}
