// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Covers the <c>whilevarvarl</c>/<c>whilevarvarn</c> loop keywords: repeats its body for as long as
    /// comparing <c>Gamevar</c> against <c>OtherGamevar</c> (both gamevars, rather than a constant) with the given
    /// <c>Condition</c> holds true.</summary>
    public sealed record WhileVarVarCommand(
        CommandList Start,
        GamevarCondition Condition,
        string Gamevar,
        string OtherGamevar)
        : LoopStructure(Start);
}
