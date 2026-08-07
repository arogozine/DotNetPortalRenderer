namespace BuildAssetLoader.Con
{
    // strength <number> — sets the actor's health; commonly a define (e.g. MYENEMY_NORMAL_STRENGTH, TOUGH).
    public sealed record StrengthCommand(string Number) : Command(CommandList.strength);
}

