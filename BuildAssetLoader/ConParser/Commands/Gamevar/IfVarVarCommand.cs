namespace BuildAssetLoader.Con
{
    /// <summary>ifvarvar&lt;cond&gt; &lt;gamevar&gt; &lt;gamevar&gt; — both operands are gamevars.</summary>
    public sealed record IfVarVarCommand(CommandList Start, GamevarCondition Condition, string Gamevar, string OtherGamevar)
        : ConditionalStructure(Start);
}

