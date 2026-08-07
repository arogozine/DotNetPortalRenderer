// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Changes the current sector of the actor identified by <c>ActorId</c> to <c>Sectnum</c>.</summary>
    [Description("changespritesect")]
    public sealed record ChangeSpriteSectCommand(
        string ActorId,
        string Sectnum) : Command(CommandList.ChangeSpriteSect);
}
