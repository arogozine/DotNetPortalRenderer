namespace BuildAssetLoader.Con
{
    // ===== Spawning =====
    // spawn/espawn/qspawn/eqspawn place <tile number> at the spawning actor's position.
    // "e" prefix sets gamevar RETURN to the new sprite's id; "q" prefix inserts it into the decal deletion queue;
    // "var" suffix takes a gamevar rather than a constant/define for <tile number>.

    public record BaseSpawnCommand(CommandList Start, string TileNumber) : Command(Start);

    public sealed record SpawnCommand(string TileNumber) : BaseSpawnCommand(CommandList.spawn, TileNumber);
    public sealed record EspawnCommand(string TileNumber) : BaseSpawnCommand(CommandList.espawn, TileNumber);
    public sealed record EspawnvarCommand(string TileNumber) : BaseSpawnCommand(CommandList.espawnvar, TileNumber);
    public sealed record QspawnCommand(string TileNumber) : BaseSpawnCommand(CommandList.qspawn, TileNumber);
    public sealed record QspawnvarCommand(string TileNumber) : BaseSpawnCommand(CommandList.qspawnvar, TileNumber);
    public sealed record EqspawnCommand(string TileNumber) : BaseSpawnCommand(CommandList.eqspawn, TileNumber);
    public sealed record EqspawnvarCommand(string TileNumber) : BaseSpawnCommand(CommandList.eqspawnvar, TileNumber);

    // ===== Materials =====

    // debris <tilenum> <amount> — tilenum is SCRAP1..SCRAP6.
    public sealed record DebrisCommand(string TileNum, string Amount) : Command(CommandList.debris);

    // guts <tilenum> <amount> — tilenum is one of the hardcoded gore tiles (JIBS1..JIBS6, HEADJIB, ...).
    public sealed record GutsCommand(string TileNum, string Amount) : Command(CommandList.guts);

    // lotsofglass <number> — spawns broken glass pieces at the current actor.
    public sealed record LotsofglassCommand(string Number) : Command(CommandList.lotsofglass);

    // mail <number> — spawns envelopes at the current actor.
    public sealed record MailCommand(string Number) : Command(CommandList.mail);

    // money <number> — spawns dollar bills at the current actor.
    public sealed record MoneyCommand(string Number) : Command(CommandList.money);

    // paper <value> — spawns pieces of paper (uses money's movement type).
    public sealed record PaperCommand(string Value) : Command(CommandList.paper);

    // ===== Projectiles =====

    // defineprojectile <tilenum> <function> <value> — declarative, placed outside actor/state code.
    public sealed record DefineprojectileCommand(string TileNum, string Function, string Value) : Command(CommandList.defineprojectile);

    // shoot/shootvar/eshoot/eshootvar <tile number> — fires a projectile from the current actor.
    public record BaseShootCommand(CommandList Start, string TileNumber) : Command(Start);

    public sealed record ShootCommand(string TileNumber) : BaseShootCommand(CommandList.shoot, TileNumber);
    public sealed record ShootvarCommand(string TileNumber) : BaseShootCommand(CommandList.shootvar, TileNumber);
    public sealed record EshootCommand(string TileNumber) : BaseShootCommand(CommandList.eshoot, TileNumber);
    public sealed record EshootvarCommand(string TileNumber) : BaseShootCommand(CommandList.eshootvar, TileNumber);

    // zshoot/zshootvar/ezshoot/ezshootvar <zvel> <tile number> — shoot with an explicit z-velocity.
    public record BaseZshootCommand(CommandList Start, string Zvel, string TileNumber) : Command(Start);

    public sealed record ZshootCommand(string Zvel, string TileNumber) : BaseZshootCommand(CommandList.zshoot, Zvel, TileNumber);
    public sealed record ZshootvarCommand(string Zvel, string TileNumber) : BaseZshootCommand(CommandList.zshootvar, Zvel, TileNumber);
    public sealed record EzshootCommand(string Zvel, string TileNumber) : BaseZshootCommand(CommandList.ezshoot, Zvel, TileNumber);
    public sealed record EzshootvarCommand(string Zvel, string TileNumber) : BaseZshootCommand(CommandList.ezshootvar, Zvel, TileNumber);
}
