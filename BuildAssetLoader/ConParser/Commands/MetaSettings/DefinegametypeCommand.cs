// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Defines a new multiplayer game type. GameTypeNum is the type's slot (0-4 are predefined, max 16),
    /// Flags is a bit field of GAMETYPE_* options (coop, weapon-stay, frag bar, etc.), and Name is the display
    /// name shown in the menu.</summary>
    [Description("definegametype")]
    public sealed record DefineGameTypeCommand(
        int GameTypeNum,
        int Flags,
        string Name) : Command(CommandList.DefineGameType);
}
