// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Conditional returning true if the current actor isn't playing any sounds. Can easily break sync in
    /// multiplayer.</summary>
    [Description("ifnosounds")]
    public sealed record IfNoSoundsCommand() : ConditionalStructure(CommandList.IfNoSounds);
}
