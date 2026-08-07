// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Sets the currently playing music position from <c>Gamevar</c>. Only safe to use with a value
    /// previously retrieved via <c>getmusicposition</c>; this command is a hack and its use is discouraged.</summary>
    [Description("setmusicposition")]
    public sealed record SetMusicPositionCommand(
        string Gamevar) : Command(CommandList.SetMusicPosition);
}
