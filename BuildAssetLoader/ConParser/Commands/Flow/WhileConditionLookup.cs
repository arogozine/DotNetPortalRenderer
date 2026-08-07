using System.Collections.Frozen;

namespace BuildAssetLoader.Con
{
    // ===== Loops =====

    public static class WhileConditionLookup
    {
        public static readonly FrozenDictionary<CommandList, GamevarCondition> Map =
            new Dictionary<CommandList, GamevarCondition>
            {
                [CommandList.whilevarl] = GamevarCondition.Less,
                [CommandList.whilevarvarl] = GamevarCondition.Less,
                [CommandList.whilevare] = GamevarCondition.Equal,
                [CommandList.whilevarn] = GamevarCondition.NotEqual,
                [CommandList.whilevarvarn] = GamevarCondition.NotEqual,
            }.ToFrozenDictionary();
    }
}

