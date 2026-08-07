// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Covers the <c>whilevarl</c>/<c>whilevare</c>/<c>whilevarn</c> loop keywords: repeats its body for
    /// as long as comparing <c>Gamevar</c> against <c>Value</c> (a constant or a define/label) with the given
    /// <c>Condition</c> holds true.</summary>
    public sealed record WhileVarCommand(
        CommandList Start,
        GamevarCondition Condition,
        string Gamevar,
        string Value)
        : LoopStructure(Start);
}
