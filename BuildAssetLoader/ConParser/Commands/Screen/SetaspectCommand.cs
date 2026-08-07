// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Sets the renderer's field of view. <c>ViewingRange</c> is the tangent of the horizontal viewing
    /// angular radius scaled by 65536 (default 65536 = 90 degree FOV under legacy aspect handling), and
    /// <c>YxAspect</c> is the Y-to-X ratio scaled by 65536 (also affects rotatesprite). Only valid in
    /// screen-drawing events.</summary>
    [Description("setaspect")]
    public sealed record SetAspectCommand(
        string ViewingRange,
        string YxAspect) : Command(CommandList.SetAspect);
}
