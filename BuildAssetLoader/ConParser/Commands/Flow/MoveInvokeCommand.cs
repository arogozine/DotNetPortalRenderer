// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Invokes a previously-declared move inside actor code, applying its velocity along with a
    /// (possibly empty) sequence of moveflags that select hardcoded movement behavior (e.g. faceplayer,
    /// seekplayer, dodgebullet).</summary>
    [Description("move")]
    public sealed record MoveInvokeCommand(
        string Name,
        string[]? MoveFlag) : Command(CommandList.Move);
}
