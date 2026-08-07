// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Makes the nearest player look down and stomp; placed in the code of the actor being stomped.</summary>
    [Description("pstomp")]
    public sealed record PStompCommand() : Command(CommandList.PStomp);
}
