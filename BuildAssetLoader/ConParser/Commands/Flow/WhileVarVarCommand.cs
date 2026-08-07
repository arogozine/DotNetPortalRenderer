namespace BuildAssetLoader.Con
{
    // whilevarvarl/whilevarvarn <gamevar> <gamevar> { ... } — both operands are gamevars.
    public sealed record WhileVarVarCommand(CommandList Start, GamevarCondition Condition, string Gamevar, string OtherGamevar)
        : LoopStructure(Start);
}

