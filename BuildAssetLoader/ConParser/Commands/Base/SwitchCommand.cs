namespace BuildAssetLoader.Con
{
    /// <summary>switch &lt;gamevar&gt; case &lt;c&gt; {...} break ... default {...} break endswitch.</summary>
    public sealed record SwitchCommand(string Gamevar, List<CaseBlock> Cases)
        : Structure(CommandList.switch_, CommandList.endswitch);
}

