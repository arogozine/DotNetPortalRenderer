// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Copies <c>Size</c> elements from <c>SrcArray</c> (starting at index <c>SrcIndex</c>) to
    /// <c>DstArray</c> (starting at index <c>DstIndex</c>).</summary>
    [Description("copy")]
    public sealed record CopyCommand(
        string SrcArray,
        string SrcIndex,
        string DstArray,
        string DstIndex,
        string Size) : Command(CommandList.Copy);
}
