namespace BuildAssetLoader.Con
{
    /// <summary>ifvar&lt;cond&gt; &lt;gamevar&gt; &lt;constant&gt; — second operand is a constant or define/label.</summary>
    public sealed record IfVarCommand(CommandList Start, GamevarCondition Condition, string Gamevar, string Value)
        : ConditionalStructure(Start);
}

