namespace BuildAssetLoader.Con
{
    // ===== Structure Access (Getters & Setters) =====
    // get<struct>[<id>].<member> <gamevar> / set<struct>[<id>].<member> <value>
    // <id> may be omitted, implying THISACTOR. "...var" siblings access per-<struct> custom gamevar members.
    // One pair per struct: actor, actorvar, input, player, playervar, projectile, sector, thisprojectile, tspr, userdef, wall.

    public sealed record GetActorCommand(string? Id, string Member, string Gamevar) : Command(CommandList.getactor);
}

