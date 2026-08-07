// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Removes the skill at index Skill from the skill menu; other skills are unaffected. If every
    /// skill ends up undefined, the skill menu is skipped and default_skill is used instead.</summary>
    [Description("undefineskill")]
    public sealed record UndefineSkillCommand(
        int Skill) : Command(CommandList.UndefineSkill);
}
