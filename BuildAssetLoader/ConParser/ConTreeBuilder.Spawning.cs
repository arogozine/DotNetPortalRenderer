namespace BuildAssetLoader.Con
{
    // Flat commands from Commands.Spawning.cs (Spawning, Materials, Projectiles).
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static Command? TryParseSpawning(CommandList command, ConTreeCursor cursor) => command switch
        {
            CommandList.Spawn => new SpawnCommand(cursor.ReadValue()),
            CommandList.ESpawn => new ESpawnCommand(cursor.ReadValue()),
            CommandList.ESpawnVar => new ESpawnVarCommand(cursor.ReadValue()),
            CommandList.QSpawn => new QSpawnCommand(cursor.ReadValue()),
            CommandList.QSpawnVar => new QSpawnVarCommand(cursor.ReadValue()),
            CommandList.EqSpawn => new EqSpawnCommand(cursor.ReadValue()),
            CommandList.EqSpawnVar => new EqSpawnVarCommand(cursor.ReadValue()),

            CommandList.Debris => new DebrisCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.Guts => new GutsCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.LotsOfGlass => new LotsOfGlassCommand(cursor.ReadValue()),
            CommandList.Mail => new MailCommand(cursor.ReadValue()),
            CommandList.Money => new MoneyCommand(cursor.ReadValue()),
            CommandList.Paper => new PaperCommand(cursor.ReadValue()),

            CommandList.DefineProjectile => new DefineProjectileCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.Shoot => new ShootCommand(cursor.ReadValue()),
            CommandList.ShootVar => new ShootVarCommand(cursor.ReadValue()),
            CommandList.EShoot => new EShootCommand(cursor.ReadValue()),
            CommandList.EShootVar => new EShootVarCommand(cursor.ReadValue()),
            CommandList.ZShoot => new ZShootCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.ZShootVar => new ZShootVarCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.EZShoot => new EZShootCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.EZShootVar => new EZShootVarCommand(cursor.ReadValue(), cursor.ReadValue()),

            _ => null,
        };
    }
}
