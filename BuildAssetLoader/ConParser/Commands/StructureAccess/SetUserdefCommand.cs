// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Assigns <c>Value</c> to a member of the userdef structure (global user preferences and game
    /// state, of which there is only one instance).</summary>
    [Description("setuserdef")]
    public sealed record SetUserDefCommand(
        string Member,
        string Value) : Command(CommandList.SetUserDef);
}
