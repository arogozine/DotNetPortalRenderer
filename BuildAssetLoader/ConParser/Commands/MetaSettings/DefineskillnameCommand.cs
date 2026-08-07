// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Names a skill level for display in the game menu. Default skills are numbered 0-3, but
    /// higher indices may be defined too; Name is limited to 32 characters.</summary>
    [Description("defineskillname")]
    public sealed record DefineSkillNameCommand(
        int Skill,
        string Name) : Command(CommandList.DefineSkillName);
}
