// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Reads the currently playing music position into <c>Gamevar</c>. The value's format is
    /// implementation/file-format-specific and may not be implemented at all in some cases; this command is a hack
    /// and its use is discouraged.</summary>
    [Description("getmusicposition")]
    public sealed record GetMusicPositionCommand(
        string Gamevar) : Command(CommandList.GetMusicPosition);
}
