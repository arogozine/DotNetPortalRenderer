// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.Collections.Frozen;

namespace BuildAssetLoader.Con
{
    /// <summary>Maps each <c>ifvar</c>/<c>ifvarvar</c> family <see cref="CommandList"/> token to the
    /// <see cref="GamevarCondition"/> it represents, so the parser can resolve which comparison a given CON
    /// keyword performs.</summary>
    public static class GamevarConditionLookup
    {
        public static readonly FrozenDictionary<CommandList, GamevarCondition> Map = new Dictionary<CommandList, GamevarCondition>
        {
            [CommandList.IfVarE] = GamevarCondition.Equal,
            [CommandList.IfVarVarE] = GamevarCondition.Equal,
            [CommandList.IfVarN] = GamevarCondition.NotEqual,
            [CommandList.IfVarVarN] = GamevarCondition.NotEqual,
            [CommandList.IfVarG] = GamevarCondition.Greater,
            [CommandList.IfVarVarG] = GamevarCondition.Greater,
            [CommandList.IfVarL] = GamevarCondition.Less,
            [CommandList.IfVarVarL] = GamevarCondition.Less,
            [CommandList.IfVarAnd] = GamevarCondition.And,
            [CommandList.IfVarVarAnd] = GamevarCondition.And,
            [CommandList.IfVarOr] = GamevarCondition.Or,
            [CommandList.IfVarVarOr] = GamevarCondition.Or,
            [CommandList.IfVarXor] = GamevarCondition.Xor,
            [CommandList.IfVarVarXor] = GamevarCondition.Xor,
            [CommandList.IfVarEither] = GamevarCondition.Either,
            [CommandList.IfVarVarEither] = GamevarCondition.Either,
        }.ToFrozenDictionary();
    }
}

