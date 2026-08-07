namespace BuildAssetLoader.Con
{
    // ===== Player Interaction =====

    // getangletotarget <returnvar> — angle to face the actor's last-known target position.
    public sealed record GetangletotargetCommand(string ReturnVar) : Command(CommandList.getangletotarget);
}

