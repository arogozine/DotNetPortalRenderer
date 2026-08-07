namespace BuildAssetLoader.Con
{
    // ===== Meta-Settings =====

    // dynamicremap — lone keyword, enables the dynamic tile remapping system.
    public sealed record DynamicremapCommand() : Command(CommandList.dynamicremap);
}

