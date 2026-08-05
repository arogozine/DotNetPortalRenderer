namespace BuildAssetLoader.Con
{
    // ===== Structure Access (Getters & Setters) =====
    // get<struct>[<id>].<member> <gamevar> / set<struct>[<id>].<member> <value>
    // <id> may be omitted, implying THISACTOR. "...var" siblings access per-<struct> custom gamevar members.
    // One pair per struct: actor, actorvar, input, player, playervar, projectile, sector, thisprojectile, tspr, userdef, wall.

    public sealed record GetActorCommand(string? Id, string Member, string Gamevar) : Command(CommandList.getactor);
    public sealed record SetActorCommand(string? Id, string Member, string Value) : Command(CommandList.setactor);

    public sealed record GetActorVarCommand(string? Id, string Member, string Gamevar) : Command(CommandList.getactorvar);
    public sealed record SetActorVarCommand(string? Id, string Member, string Value) : Command(CommandList.setactorvar);

    public sealed record GetInputCommand(string? Id, string Member, string Gamevar) : Command(CommandList.getinput);
    public sealed record SetInputCommand(string? Id, string Member, string Value) : Command(CommandList.setinput);

    public sealed record GetPlayerCommand(string? Id, string Member, string Gamevar) : Command(CommandList.getplayer);
    public sealed record SetPlayerCommand(string? Id, string Member, string Value) : Command(CommandList.setplayer);

    public sealed record GetPlayerVarCommand(string? Id, string Member, string Gamevar) : Command(CommandList.getplayervar);
    public sealed record SetPlayerVarCommand(string? Id, string Member, string Value) : Command(CommandList.setplayervar);

    public sealed record GetProjectileCommand(string? Id, string Member, string Gamevar) : Command(CommandList.getprojectile);
    public sealed record SetProjectileCommand(string? Id, string Member, string Value) : Command(CommandList.setprojectile);

    public sealed record GetSectorCommand(string? Id, string Member, string Gamevar) : Command(CommandList.getsector);
    public sealed record SetSectorCommand(string? Id, string Member, string Value) : Command(CommandList.setsector);

    public sealed record GetThisProjectileCommand(string? Id, string Member, string Gamevar) : Command(CommandList.getthisprojectile);
    public sealed record SetThisProjectileCommand(string? Id, string Member, string Value) : Command(CommandList.setthisprojectile);

    public sealed record GetTsprCommand(string? Id, string Member, string Gamevar) : Command(CommandList.gettspr);
    public sealed record SetTsprCommand(string? Id, string Member, string Value) : Command(CommandList.settspr);

    // userdef has no id — the brackets are always empty ("userdef[].<member>").
    public sealed record GetUserdefCommand(string Member, string Gamevar) : Command(CommandList.getuserdef);
    public sealed record SetUserdefCommand(string Member, string Value) : Command(CommandList.setuserdef);

    public sealed record GetWallCommand(string? Id, string Member, string Gamevar) : Command(CommandList.getwall);
    public sealed record SetWallCommand(string? Id, string Member, string Value) : Command(CommandList.setwall);

    // ===== Single-Use Structure Access (deprecated shortcuts, no struct-index syntax) =====

    // getactorangle <gamevar> — deprecated; gets the current actor's angle.
    public sealed record GetactorangleCommand(string Gamevar) : Command(CommandList.getactorangle);

    // getplayerangle <gamevar> — deprecated; gets the current player's angle.
    public sealed record GetplayerangleCommand(string Gamevar) : Command(CommandList.getplayerangle);

    // gettextureceiling — retrieves the current actor's sector's ceiling texture into gamevar RETURN.
    public sealed record GettextureceilingCommand() : Command(CommandList.gettextureceiling);

    // gettexturefloor — retrieves the current actor's sector's floor texture into gamevar RETURN.
    public sealed record GettexturefloorCommand() : Command(CommandList.gettexturefloor);

    // sectgethitag — deprecated; current sector's hitag into per-actor gamevar HITAG.
    public sealed record SectgethitagCommand() : Command(CommandList.sectgethitag);

    // sectgetlotag — deprecated; current sector's lotag into per-actor gamevar LOTAG.
    public sealed record SectgetlotagCommand() : Command(CommandList.sectgetlotag);

    // spgethitag — deprecated; current actor's hitag into per-actor gamevar HITAG.
    public sealed record SpgethitagCommand() : Command(CommandList.spgethitag);

    // spgetlotag — deprecated; current actor's lotag into per-actor gamevar LOTAG.
    public sealed record SpgetlotagCommand() : Command(CommandList.spgetlotag);

    // setactorangle <gamevar> — deprecated; sets the current actor's angle.
    public sealed record SetactorangleCommand(string Gamevar) : Command(CommandList.setactorangle);

    // setplayerangle <gamevar> — deprecated; sets the current player's angle.
    public sealed record SetplayerangleCommand(string Gamevar) : Command(CommandList.setplayerangle);
}
