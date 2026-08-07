// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Plays a sound (by number/defined name, not filename) that can be heard from anywhere in the
    /// map.</summary>
    [Description("globalsound")]
    public sealed record GlobalSoundCommand(
        string Sound) : BaseSoundCommand(CommandList.GlobalSound, Sound);
}
