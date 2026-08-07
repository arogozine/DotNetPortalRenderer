using System.Collections.Frozen;

namespace BuildAssetLoader.Con
{
    public static class GamevarConditionLookup
    {
        public static readonly FrozenDictionary<CommandList, GamevarCondition> Map = new Dictionary<CommandList, GamevarCondition>
        {
            [CommandList.ifvare] = GamevarCondition.Equal,
            [CommandList.ifvarvare] = GamevarCondition.Equal,
            [CommandList.ifvarn] = GamevarCondition.NotEqual,
            [CommandList.ifvarvarn] = GamevarCondition.NotEqual,
            [CommandList.ifvarg] = GamevarCondition.Greater,
            [CommandList.ifvarvarg] = GamevarCondition.Greater,
            [CommandList.ifvarl] = GamevarCondition.Less,
            [CommandList.ifvarvarl] = GamevarCondition.Less,
            [CommandList.ifvarand] = GamevarCondition.And,
            [CommandList.ifvarvarand] = GamevarCondition.And,
            [CommandList.ifvaror] = GamevarCondition.Or,
            [CommandList.ifvarvaror] = GamevarCondition.Or,
            [CommandList.ifvarxor] = GamevarCondition.Xor,
            [CommandList.ifvarvarxor] = GamevarCondition.Xor,
            [CommandList.ifvareither] = GamevarCondition.Either,
            [CommandList.ifvarvareither] = GamevarCondition.Either,
        }.ToFrozenDictionary();
    }
}

