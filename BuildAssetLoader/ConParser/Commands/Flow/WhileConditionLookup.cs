// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.Collections.Frozen;

namespace BuildAssetLoader.Con
{
    // ===== Loops =====

    /// <summary>Maps each <c>while*</c> loop keyword to the comparison it performs against its operands (e.g.
    /// <c>whilevarl</c> tests "less than"). Used to resolve the <see cref="GamevarCondition"/> for a loop from its
    /// starting <see cref="CommandList"/> token.</summary>
    public static class WhileConditionLookup
    {
        public static readonly FrozenDictionary<CommandList, GamevarCondition> Map =
            new Dictionary<CommandList, GamevarCondition>
            {
                [CommandList.WhileVarL] = GamevarCondition.Less,
                [CommandList.WhileVarVarL] = GamevarCondition.Less,
                [CommandList.WhileVarE] = GamevarCondition.Equal,
                [CommandList.WhileVarN] = GamevarCondition.NotEqual,
                [CommandList.WhileVarVarN] = GamevarCondition.NotEqual,
            }.ToFrozenDictionary();
    }
}
