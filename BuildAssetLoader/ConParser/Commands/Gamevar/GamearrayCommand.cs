// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Declares a gamearray: a named, fixed-size collection of gamevar-like values addressed by index
    /// (<c>Name</c>[index]). <c>Size</c> is the number of elements, all initialized to 0; <c>Flags</c> is an
    /// optional bitmask (e.g. element width/signedness, whether the array persists in saved map state) selecting
    /// how the array's storage and behavior are configured.</summary>
    [Description("gamearray")]
    public sealed record GameArrayCommand(
        string Name,
        int Size,
        int? Flags) : Command(CommandList.GameArray);
}
