namespace BuildAssetLoader.Con
{
    // Leaf construction for if* commands (ConditionalStructure); body/else attachment happens in ConTreeBuilder.cs.
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static ConditionalStructure ParseConditionalLeaf(CommandList command, ConTreeCursor cursor) => command switch
        {
            // Meta-Settings - If (Commands.MetaSettings.cs)
            CommandList.ifrespawn => new IfrespawnCommand(),
            CommandList.ifmultiplayer => new IfmultiplayerCommand(),
            CommandList.ifclient => new IfclientCommand(),
            CommandList.ifserver => new IfserverCommand(),

            // Gamevar Conditions (Commands.cs)
            CommandList.ifvare or CommandList.ifvarn or CommandList.ifvarg or CommandList.ifvarl
                or CommandList.ifvarand or CommandList.ifvaror or CommandList.ifvarxor or CommandList.ifvareither =>
                new IfVarCommand(command, GamevarConditionLookup.Map[command], cursor.ReadValue(), cursor.ReadValue()),
            CommandList.ifvarvare or CommandList.ifvarvarn or CommandList.ifvarvarg or CommandList.ifvarvarl
                or CommandList.ifvarvarand or CommandList.ifvarvaror or CommandList.ifvarvarxor or CommandList.ifvarvareither =>
                new IfVarVarCommand(command, GamevarConditionLookup.Map[command], cursor.ReadValue(), cursor.ReadValue()),

            // Actor If (Commands.Conditionals.cs)
            CommandList.ifactor => new IfactorCommand(cursor.ReadValue()),
            CommandList.ifaction => new IfactionCommand(cursor.ReadValue()),
            CommandList.ifactioncount => new IfactioncountCommand(cursor.ReadValue()),
            CommandList.ifai => new IfaiCommand(cursor.ReadValue()),
            CommandList.ifcount => new IfcountCommand(cursor.ReadValue()),
            CommandList.ifmove => new IfmoveCommand(cursor.ReadValue()),
            CommandList.ifspawnedby => new IfspawnedbyCommand(cursor.ReadValue()),
            CommandList.ifspritepal => new IfspritepalCommand(cursor.ReadValue()),
            CommandList.ifstrength => new IfstrengthCommand(cursor.ReadValue()),
            CommandList.ifhitweapon => new IfhitweaponCommand(),
            CommandList.ifwasweapon => new IfwasweaponCommand(cursor.ReadValue()),
            CommandList.ifdead => new IfdeadCommand(),
            CommandList.ifactornotstayput => new IfactornotstayputCommand(),

            // Surroundings If
            CommandList.ifawayfromwall => new IfawayfromwallCommand(),
            CommandList.ifbulletnear => new IfbulletnearCommand(),
            CommandList.ifceilingdistl => new IfceilingdistlCommand(cursor.ReadValue()),
            CommandList.iffloordistl => new IffloordistlCommand(cursor.ReadValue()),
            CommandList.ifgapzl => new IfgapzlCommand(cursor.ReadValue()),
            CommandList.ifsquished => new IfsquishedCommand(),
            CommandList.ifnotmoving => new IfnotmovingCommand(),
            CommandList.ifinwater => new IfinwaterCommand(),
            CommandList.ifonwater => new IfonwaterCommand(),
            CommandList.ifoutside => new IfoutsideCommand(),
            CommandList.ifinspace => new IfinspaceCommand(),
            CommandList.ifinouterspace => new IfinouterspaceCommand(),
            CommandList.ifrnd => new IfrndCommand(cursor.ReadValue()),

            // Player Interaction If
            CommandList.ifangdiffl => new IfangdifflCommand(cursor.ReadValue()),
            CommandList.ifcansee => new IfcanseeCommand(),
            CommandList.ifcanseetarget => new IfcanseetargetCommand(),
            CommandList.ifcanshoottarget => new IfcanshoottargetCommand(),
            CommandList.ifhitspace => new IfhitspaceCommand(),

            // Player If
            CommandList.ifgotweaponce => new IfgotweaponceCommand(cursor.ReadValue()),
            CommandList.ifp => new IfpCommand(cursor.ReadAllContiguousValues()),
            CommandList.ifpdistg => new IfpdistgCommand(cursor.ReadValue()),
            CommandList.ifpdistl => new IfpdistlCommand(cursor.ReadValue()),
            CommandList.ifphealthl => new IfphealthlCommand(cursor.ReadValue()),
            CommandList.ifpinventory => new IfpinventoryCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.ifplayersl => new IfplayerslCommand(cursor.ReadValue()),

            // Audio/Cutscene If
            CommandList.ifsound => new IfsoundCommand(cursor.ReadValue()),
            CommandList.ifactorsound => new IfactorsoundCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.ifnosounds => new IfnosoundsCommand(),
            CommandList.ifcutscene => new IfcutsceneCommand(cursor.ReadInt()),

            _ => throw new FormatException($"No conditional-command mapping for '{command}'."),
        };
    }
}
