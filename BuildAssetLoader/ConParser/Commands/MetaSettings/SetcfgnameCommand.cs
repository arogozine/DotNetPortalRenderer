// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Specifies a custom config file name for storing settings and gamevars, intended to mirror the
    /// <c>-cfg</c> command-line parameter. In practice display/rendering settings can't be restored this way
    /// since the startup window reads eduke32.cfg before CON code is parsed; the <c>-cfg</c> parameter is a
    /// more reliable alternative.</summary>
    [Description("setcfgname")]
    public sealed record SetCfgNameCommand(
        string CfgName) : Command(CommandList.SetCfgName);
}
