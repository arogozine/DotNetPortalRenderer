// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Mapping Features =====

    /// <summary>Plays the sound whose number matches the executing actor's yvel. For example, setting yvel to 12
    /// before calling this plays sound #12.</summary>
    [Description("mikesnd")]
    public sealed record MikeSndCommand() : Command(CommandList.MikeSnd);
}
