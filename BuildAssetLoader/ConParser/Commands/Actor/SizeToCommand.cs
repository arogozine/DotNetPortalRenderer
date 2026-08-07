// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Gradually resizes the current actor's sprite towards (<c>XRepeat</c>, <c>YRepeat</c>); the default
    /// size is 64x64. Calling this repeatedly increases the speed of the resize. See also the instantaneous version,
    /// <c>sizeat</c>.</summary>
    [Description("sizeto")]
    public sealed record SizeToCommand(
        string XRepeat,
        string YRepeat) : Command(CommandList.SizeTo);
}
