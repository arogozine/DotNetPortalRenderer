// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.Collections.Frozen;

namespace BuildAssetLoader.Con
{
    /// <summary>Maps each <c>*var</c>/<c>*varvar</c> family <see cref="CommandList"/> token to the
    /// <see cref="GamevarOperator"/> it represents, so the parser can resolve which binary operation a given
    /// CON keyword performs.</summary>
    public static class GamevarOperatorLookup
    {
        public static readonly FrozenDictionary<CommandList, GamevarOperator> Map = new Dictionary<CommandList, GamevarOperator>
        {
            [CommandList.SetVar] = GamevarOperator.Set,
            [CommandList.SetVarVar] = GamevarOperator.Set,
            [CommandList.AddVar] = GamevarOperator.Add,
            [CommandList.AddVarVar] = GamevarOperator.Add,
            [CommandList.SubVar] = GamevarOperator.Sub,
            [CommandList.SubVarVar] = GamevarOperator.Sub,
            [CommandList.MulVar] = GamevarOperator.Mul,
            [CommandList.MulVarVar] = GamevarOperator.Mul,
            [CommandList.DivVar] = GamevarOperator.Div,
            [CommandList.DivVarVar] = GamevarOperator.Div,
            [CommandList.ModVar] = GamevarOperator.Mod,
            [CommandList.ModVarVar] = GamevarOperator.Mod,
            [CommandList.AndVar] = GamevarOperator.And,
            [CommandList.AndVarVar] = GamevarOperator.And,
            [CommandList.OrVar] = GamevarOperator.Or,
            [CommandList.OrVarVar] = GamevarOperator.Or,
            [CommandList.XorVar] = GamevarOperator.Xor,
            [CommandList.XorVarVar] = GamevarOperator.Xor,
            [CommandList.RandVar] = GamevarOperator.Rand,
            [CommandList.RandVarVar] = GamevarOperator.Rand,
        }.ToFrozenDictionary();
    }
}

