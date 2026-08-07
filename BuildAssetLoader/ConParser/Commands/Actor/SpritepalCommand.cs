namespace BuildAssetLoader.Con
{
    // spritepal <number> — changes the actor's palette reference number.
    public sealed record SpritepalCommand(string Number) : Command(CommandList.spritepal);
}

