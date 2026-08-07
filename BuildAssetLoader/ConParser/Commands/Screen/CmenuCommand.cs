namespace BuildAssetLoader.Con
{
    // cmenu <value> — opens a specific menu screen (see current_menu / MENU_* defines).
    public sealed record CmenuCommand(string Value) : Command(CommandList.cmenu);
}

