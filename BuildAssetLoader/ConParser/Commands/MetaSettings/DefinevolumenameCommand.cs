// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Names an episode for the new-game menu and implicitly sets the total episode count to
    /// EpisodeNumber (starting at 0, max 6).</summary>
    [Description("definevolumename")]
    public sealed record DefineVolumeNameCommand(
        int EpisodeNumber,
        string Name) : Command(CommandList.DefineVolumeName);
}
