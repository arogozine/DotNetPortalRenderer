namespace BuildAssetLoader.Con
{
    // ===== Actor If =====

    public sealed record IfactorCommand(string TileNum) : ConditionalStructure(CommandList.ifactor);

    public sealed record IfactionCommand(string Action) : ConditionalStructure(CommandList.ifaction);

    public sealed record IfactioncountCommand(string Number) : ConditionalStructure(CommandList.ifactioncount);

    public sealed record IfaiCommand(string Ai) : ConditionalStructure(CommandList.ifai);

    // <number> is frequently a define (e.g. SHRUNKDONECOUNT, THAWTIME) rather than a literal.
    public sealed record IfcountCommand(string Number) : ConditionalStructure(CommandList.ifcount);

    public sealed record IfmoveCommand(string Move) : ConditionalStructure(CommandList.ifmove);

    public sealed record IfspawnedbyCommand(string Actor) : ConditionalStructure(CommandList.ifspawnedby);

    public sealed record IfspritepalCommand(string Pal) : ConditionalStructure(CommandList.ifspritepal);

    public sealed record IfstrengthCommand(string Strength) : ConditionalStructure(CommandList.ifstrength);

    public sealed record IfhitweaponCommand() : ConditionalStructure(CommandList.ifhitweapon);

    public sealed record IfwasweaponCommand(string Weapon) : ConditionalStructure(CommandList.ifwasweapon);

    public sealed record IfdeadCommand() : ConditionalStructure(CommandList.ifdead);

    public sealed record IfactornotstayputCommand() : ConditionalStructure(CommandList.ifactornotstayput);

    // ===== Surroundings If =====

    public sealed record IfawayfromwallCommand() : ConditionalStructure(CommandList.ifawayfromwall);

    public sealed record IfbulletnearCommand() : ConditionalStructure(CommandList.ifbulletnear);

    public sealed record IfceilingdistlCommand(string Number) : ConditionalStructure(CommandList.ifceilingdistl);

    public sealed record IffloordistlCommand(string Number) : ConditionalStructure(CommandList.iffloordistl);

    public sealed record IfgapzlCommand(string Number) : ConditionalStructure(CommandList.ifgapzl);

    public sealed record IfsquishedCommand() : ConditionalStructure(CommandList.ifsquished);

    public sealed record IfnotmovingCommand() : ConditionalStructure(CommandList.ifnotmoving);

    public sealed record IfinwaterCommand() : ConditionalStructure(CommandList.ifinwater);

    public sealed record IfonwaterCommand() : ConditionalStructure(CommandList.ifonwater);

    public sealed record IfoutsideCommand() : ConditionalStructure(CommandList.ifoutside);

    public sealed record IfinspaceCommand() : ConditionalStructure(CommandList.ifinspace);

    public sealed record IfinouterspaceCommand() : ConditionalStructure(CommandList.ifinouterspace);

    // ifrnd <value> — value in [-1, 255]; -1 always takes else, >=255 always takes if. Commonly a define (e.g. SWEARFREQUENCY).
    public sealed record IfrndCommand(string Value) : ConditionalStructure(CommandList.ifrnd);

    // ===== Player Interaction If =====

    public sealed record IfangdifflCommand(string Number) : ConditionalStructure(CommandList.ifangdiffl);

    public sealed record IfcanseeCommand() : ConditionalStructure(CommandList.ifcansee);

    public sealed record IfcanseetargetCommand() : ConditionalStructure(CommandList.ifcanseetarget);

    public sealed record IfcanshoottargetCommand() : ConditionalStructure(CommandList.ifcanshoottarget);

    public sealed record IfhitspaceCommand() : ConditionalStructure(CommandList.ifhitspace);

    // ===== Player If =====

    // ifgotweaponce <number> — number is a weapon id define.
    public sealed record IfgotweaponceCommand(string Number) : ConditionalStructure(CommandList.ifgotweaponce);

    // ifp <condition1> [<condition2> ...] — each condition is one of the predefined p* flag names
    // (pstanding, pwalking, ...); true if any of them match (logical OR).
    public sealed record IfpCommand(string[] Conditions) : ConditionalStructure(CommandList.ifp);

    // <number> is frequently a define (e.g. SQUISHABLEDISTANCE, FROZENQUICKKICKDIST).
    public sealed record IfpdistgCommand(string Number) : ConditionalStructure(CommandList.ifpdistg);

    public sealed record IfpdistlCommand(string Number) : ConditionalStructure(CommandList.ifpdistl);

    public sealed record IfphealthlCommand(string Number) : ConditionalStructure(CommandList.ifphealthl);

    // ifpinventory <item> <value> — item is an inventory index (GET_* define); value is often a define too.
    public sealed record IfpinventoryCommand(string Item, string Value) : ConditionalStructure(CommandList.ifpinventory);

    public sealed record IfplayerslCommand(string Value) : ConditionalStructure(CommandList.ifplayersl);

    // ===== Audio/Cutscene If =====

    // ifsound <sound> — sound is a sound label define.
    public sealed record IfsoundCommand(string Sound) : ConditionalStructure(CommandList.ifsound);

    // ifactorsound <sprite ID> <sound#>
    public sealed record IfactorsoundCommand(string SpriteId, string Sound) : ConditionalStructure(CommandList.ifactorsound);

    public sealed record IfnosoundsCommand() : ConditionalStructure(CommandList.ifnosounds);

    // ifcutscene <cutscene path> — argument is a quote id holding the cutscene path.
    public sealed record IfcutsceneCommand(int QuoteId) : ConditionalStructure(CommandList.ifcutscene);

    // ===== Gamevar Conditions =====
    // Modeled by the enum-driven IfVarCommand / IfVarVarCommand in Commands.cs (see GamevarCondition, §3.5):
    //   ifvare, ifvarn, ifvarg, ifvarl, ifvarand, ifvaror, ifvarxor, ifvareither       -> IfVarCommand
    //   ifvarvare, ifvarvarn, ifvarvarg, ifvarvarl, ifvarvarand, ifvarvaror,
    //   ifvarvarxor, ifvarvareither                                                    -> IfVarVarCommand
}
