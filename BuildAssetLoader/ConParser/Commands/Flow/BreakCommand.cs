namespace BuildAssetLoader.Con
{
    // CommandList.else_ is consumed into ConditionalStructure.ElseBody — no standalone record (see §5).

    // ===== Termination =====

    public sealed record BreakCommand() : Command(CommandList.break_);
}

