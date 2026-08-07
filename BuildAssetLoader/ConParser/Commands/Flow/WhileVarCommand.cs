namespace BuildAssetLoader.Con
{
    // whilevarl/whilevare/whilevarn <gamevar> <value> { ... } — second operand is a constant or define/label.
    public sealed record WhileVarCommand(CommandList Start, GamevarCondition Condition, string Gamevar, string Value)
        : LoopStructure(Start);
}

