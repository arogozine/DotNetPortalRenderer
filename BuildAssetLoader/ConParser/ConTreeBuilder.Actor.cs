namespace BuildAssetLoader.Con
{
    // Flat commands from Commands.cs (Actors - Structures) and Commands.Actor.cs.
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static Command? TryParseActor(CommandList command, ConTreeCursor cursor) => command switch
        {
            CommandList.CActor => new CActorCommand(cursor.ReadValue()),

            CommandList.Count => new CountCommand(cursor.ReadValue()),
            CommandList.ResetActionCount => new ResetActionCountCommand(),
            CommandList.ResetCount => new ResetCountCommand(),
            CommandList.CStat => new CStatCommand(cursor.ReadValue()),
            CommandList.CStatOr => new CStatOrCommand(cursor.ReadValue()),
            CommandList.ClipDist => new ClipDistCommand(cursor.ReadValue()),
            CommandList.SizeAt => new SizeAtCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.SizeTo => new SizeToCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.Strength => new StrengthCommand(cursor.ReadValue()),
            CommandList.AddStrength => new AddStrengthCommand(cursor.ReadValue()),
            CommandList.SpritePal => new SpritePalCommand(cursor.ReadValue()),
            CommandList.GetLastPal => new GetLastPalCommand(),
            CommandList.SleepTime => new SleepTimeCommand(cursor.ReadValue()),
            CommandList.SpriteFlags => ParseSpriteflags(cursor),
            CommandList.AngOff => new AngOffCommand(cursor.ReadValue()),
            CommandList.AngOffVar => new AngOffVarCommand(cursor.ReadValue()),
            CommandList.ChangeSpriteSect => new ChangeSpriteSectCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.ChangeSpriteStat => new ChangeSpriteStatCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.SetSprite => new SetSpriteCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            CommandList.Fall => new FallCommand(),
            CommandList.InsertSpriteQ => new InsertSpriteQCommand(),
            CommandList.KillIt => new KillItCommand(),
            CommandList.MoveSprite => new MoveSpriteCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.Ssp => new SspCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.ClipMove => new ClipMoveCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.ClipMoveNoSlide => new ClipMoveNoSlideCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            CommandList.Dist => new DistCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.LDist => new LDistCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.CanSee => new CanSeeCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.CanSeeSpr => new CanSeeSprCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            CommandList.HitRadius => new HitRadiusCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.HitRadiusVar => new HitRadiusVarCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.Flash => new FlashCommand(),

            CommandList.MikeSnd => new MikeSndCommand(),
            CommandList.RespawnHitag => new RespawnHitagCommand(),

            CommandList.GetAngleToTarget => new GetAngleToTargetCommand(cursor.ReadValue()),

            _ => null,
        };

        private static SpriteFlagsCommand ParseSpriteflags(ConTreeCursor cursor)
        {
            int n = cursor.CountContiguousValues();

            if (n >= 2)
            {
                string[] args = cursor.ReadValues(2);
                return new SpriteFlagsCommand(args[0], args[1]);
            }

            return new SpriteFlagsCommand(null, cursor.ReadValue());
        }
    }
}
