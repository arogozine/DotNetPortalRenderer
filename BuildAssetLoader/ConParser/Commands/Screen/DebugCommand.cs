namespace BuildAssetLoader.Con
{
    // ===== Debug =====

    // debug <number> — logs <number> and triggers a debugger breakpoint in non-release builds.
    public sealed record DebugCommand(string Number) : Command(CommandList.debug);
}

