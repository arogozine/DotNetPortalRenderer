namespace BuildAssetLoader.Con
{
    // ===== Gamevar operator / condition records (Phase 4/3, declared here alongside the enums per §3.5) =====

    /// <summary>&lt;op&gt; &lt;gamevar&gt; &lt;constant|gamevar&gt; — second operand is a constant or a define/label (setvar, addvar, ...).</summary>
    public sealed record VarOpCommand(CommandList Start, GamevarOperator Operator, string Gamevar, string Value)
        : Command(Start);
}

