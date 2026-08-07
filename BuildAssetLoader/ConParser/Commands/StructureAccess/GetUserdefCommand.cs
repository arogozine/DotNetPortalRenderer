// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // userdef has no id — the brackets are always empty ("userdef[].<member>").

    /// <summary>Reads a member of the userdef structure (global user preferences and game state, of which there is
    /// only one instance) into <c>Gamevar</c>.</summary>
    [Description("getuserdef")]
    public sealed record GetUserDefCommand(
        string Member,
        string Gamevar) : Command(CommandList.GetUserDef);
}
