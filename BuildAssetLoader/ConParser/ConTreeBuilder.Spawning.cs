namespace BuildAssetLoader.Con
{
    // Flat commands from Commands.Spawning.cs (Spawning, Materials, Projectiles).
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static Command? TryParseSpawning(CommandList command, ConTreeCursor cursor) => command switch
        {
            CommandList.spawn => new SpawnCommand(cursor.ReadValue()),
            CommandList.espawn => new EspawnCommand(cursor.ReadValue()),
            CommandList.espawnvar => new EspawnvarCommand(cursor.ReadValue()),
            CommandList.qspawn => new QspawnCommand(cursor.ReadValue()),
            CommandList.qspawnvar => new QspawnvarCommand(cursor.ReadValue()),
            CommandList.eqspawn => new EqspawnCommand(cursor.ReadValue()),
            CommandList.eqspawnvar => new EqspawnvarCommand(cursor.ReadValue()),

            CommandList.debris => new DebrisCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.guts => new GutsCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.lotsofglass => new LotsofglassCommand(cursor.ReadValue()),
            CommandList.mail => new MailCommand(cursor.ReadValue()),
            CommandList.money => new MoneyCommand(cursor.ReadValue()),
            CommandList.paper => new PaperCommand(cursor.ReadValue()),

            CommandList.defineprojectile => new DefineprojectileCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.shoot => new ShootCommand(cursor.ReadValue()),
            CommandList.shootvar => new ShootvarCommand(cursor.ReadValue()),
            CommandList.eshoot => new EshootCommand(cursor.ReadValue()),
            CommandList.eshootvar => new EshootvarCommand(cursor.ReadValue()),
            CommandList.zshoot => new ZshootCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.zshootvar => new ZshootvarCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.ezshoot => new EzshootCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.ezshootvar => new EzshootvarCommand(cursor.ReadValue(), cursor.ReadValue()),

            _ => null,
        };
    }
}
