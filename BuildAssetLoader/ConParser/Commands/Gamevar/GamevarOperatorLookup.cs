using System.Collections.Frozen;

namespace BuildAssetLoader.Con
{
    public static class GamevarOperatorLookup
    {
        public static readonly FrozenDictionary<CommandList, GamevarOperator> Map = new Dictionary<CommandList, GamevarOperator>
        {
            [CommandList.setvar] = GamevarOperator.Set,
            [CommandList.setvarvar] = GamevarOperator.Set,
            [CommandList.addvar] = GamevarOperator.Add,
            [CommandList.addvarvar] = GamevarOperator.Add,
            [CommandList.subvar] = GamevarOperator.Sub,
            [CommandList.subvarvar] = GamevarOperator.Sub,
            [CommandList.mulvar] = GamevarOperator.Mul,
            [CommandList.mulvarvar] = GamevarOperator.Mul,
            [CommandList.divvar] = GamevarOperator.Div,
            [CommandList.divvarvar] = GamevarOperator.Div,
            [CommandList.modvar] = GamevarOperator.Mod,
            [CommandList.modvarvar] = GamevarOperator.Mod,
            [CommandList.andvar] = GamevarOperator.And,
            [CommandList.andvarvar] = GamevarOperator.And,
            [CommandList.orvar] = GamevarOperator.Or,
            [CommandList.orvarvar] = GamevarOperator.Or,
            [CommandList.xorvar] = GamevarOperator.Xor,
            [CommandList.xorvarvar] = GamevarOperator.Xor,
            [CommandList.randvar] = GamevarOperator.Rand,
            [CommandList.randvarvar] = GamevarOperator.Rand,
        }.ToFrozenDictionary();
    }
}

