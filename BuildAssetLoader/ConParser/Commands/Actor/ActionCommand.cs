namespace BuildAssetLoader.Con
{
    // ===== Global Settings - Object-Oriented =====

    public record ActionCommand(string Name, int? Startframe, int? Frames, int? ViewType, int? IncValue, int? Delay)
         : Command(CommandList.action);
}

