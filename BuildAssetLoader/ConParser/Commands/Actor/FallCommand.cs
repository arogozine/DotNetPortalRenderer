namespace BuildAssetLoader.Con
{
    // ===== Commands =====

    // fall — begins the actor falling under gravity; also applies impact damage on landing.
    public sealed record FallCommand() : Command(CommandList.fall);
}

