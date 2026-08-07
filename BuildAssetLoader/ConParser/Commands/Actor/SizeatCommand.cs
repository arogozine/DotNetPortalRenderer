// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Instantly resizes the current actor's sprite to (<c>XRepeat</c>, <c>YRepeat</c>); the default size
    /// is 64x64. Setting either repeat to 0 deletes the actor, equivalent to <c>killit</c>. See also the gradual
    /// version, <c>sizeto</c>.</summary>
    [Description("sizeat")]
    public sealed record SizeAtCommand(string XRepeat, string YRepeat) : Command(CommandList.SizeAt);
}
