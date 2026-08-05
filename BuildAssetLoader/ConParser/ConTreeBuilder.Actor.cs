namespace BuildAssetLoader.Con
{
    // Flat commands from Commands.cs (Actors - Structures) and Commands.Actor.cs.
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static Command? TryParseActor(CommandList command, ConTreeCursor cursor) => command switch
        {
            CommandList.cactor => new CActorCommand(cursor.ReadValue()),

            CommandList.count => new CountCommand(cursor.ReadValue()),
            CommandList.resetactioncount => new ResetactioncountCommand(),
            CommandList.resetcount => new ResetcountCommand(),
            CommandList.cstat => new CstatCommand(cursor.ReadValue()),
            CommandList.cstator => new CstatorCommand(cursor.ReadValue()),
            CommandList.clipdist => new ClipdistCommand(cursor.ReadValue()),
            CommandList.sizeat => new SizeatCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.sizeto => new SizetoCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.strength => new StrengthCommand(cursor.ReadValue()),
            CommandList.addstrength => new AddstrengthCommand(cursor.ReadValue()),
            CommandList.spritepal => new SpritepalCommand(cursor.ReadValue()),
            CommandList.getlastpal => new GetlastpalCommand(),
            CommandList.sleeptime => new SleeptimeCommand(cursor.ReadValue()),
            CommandList.spriteflags => ParseSpriteflags(cursor),
            CommandList.angoff => new AngoffCommand(cursor.ReadValue()),
            CommandList.angoffvar => new AngoffvarCommand(cursor.ReadValue()),
            CommandList.changespritesect => new ChangespritesectCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.changespritestat => new ChangespritestatCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.setsprite => new SetspriteCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            CommandList.fall => new FallCommand(),
            CommandList.insertspriteq => new InsertspriteqCommand(),
            CommandList.killit => new KillitCommand(),
            CommandList.movesprite => new MovespriteCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.ssp => new SspCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.clipmove => new ClipmoveCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.clipmovenoslide => new ClipmovenoslideCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            CommandList.dist => new DistCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.ldist => new LdistCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.cansee => new CanseeCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.canseespr => new CanseesprCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            CommandList.hitradius => new HitradiusCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.hitradiusvar => new HitradiusVarCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.flash => new FlashCommand(),

            CommandList.mikesnd => new MikesndCommand(),
            CommandList.respawnhitag => new RespawnhitagCommand(),

            CommandList.getangletotarget => new GetangletotargetCommand(cursor.ReadValue()),

            _ => null,
        };

        private static SpriteflagsCommand ParseSpriteflags(ConTreeCursor cursor)
        {
            int n = cursor.CountContiguousValues();

            if (n >= 2)
            {
                string[] args = cursor.ReadValues(2);
                return new SpriteflagsCommand(args[0], args[1]);
            }

            return new SpriteflagsCommand(null, cursor.ReadValue());
        }
    }
}
