namespace BuildAssetLoader.Con
{
    /// <summary>&lt;op&gt;varvar &lt;gamevar&gt; &lt;gamevar&gt; — second operand is itself a gamevar (setvarvar, addvarvar, ...).</summary>
    public sealed record VarVarOpCommand(CommandList Start, GamevarOperator Operator, string Gamevar, string OtherGamevar)
        : Command(Start);
}

