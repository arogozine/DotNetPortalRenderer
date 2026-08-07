namespace BuildAssetLoader.Con
{
    // ===== Game-Changing =====

    // activatecheat <cheat_id> — singleplayer only; also fires EVENT_ACTIVATECHEAT.
    public sealed record ActivatecheatCommand(string CheatId) : Command(CommandList.activatecheat);
}

