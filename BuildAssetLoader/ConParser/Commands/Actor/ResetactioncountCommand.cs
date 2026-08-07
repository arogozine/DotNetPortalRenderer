namespace BuildAssetLoader.Con
{
    // resetactioncount — resets actioncount to 0, restarting the current action.
    public sealed record ResetactioncountCommand() : Command(CommandList.resetactioncount);
}

