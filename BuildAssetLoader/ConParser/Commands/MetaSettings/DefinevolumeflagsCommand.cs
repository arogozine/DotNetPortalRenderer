// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Declarative statement (placed outside executed code blocks) that assigns a bit field of
    /// EF_* flags to a volume index, e.g. hiding the episode from the singleplayer menu.</summary>
    [Description("definevolumeflags")]
    public sealed record DefineVolumeFlagsCommand(
        int Volume,
        int Flags) : Command(CommandList.DefineVolumeFlags);
}
