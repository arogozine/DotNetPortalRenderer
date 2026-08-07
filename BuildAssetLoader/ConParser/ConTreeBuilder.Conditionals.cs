namespace BuildAssetLoader.Con
{
    // Leaf construction for if* commands (ConditionalStructure); body/else attachment happens in ConTreeBuilder.cs.
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static ConditionalStructure ParseConditionalLeaf(CommandList command, ConTreeCursor cursor) => command switch
        {
            // Meta-Settings - If (Commands.MetaSettings.cs)
            CommandList.IfRespawn => new IfRespawnCommand(),
            CommandList.IfMultiplayer => new IfMultiplayerCommand(),
            CommandList.IfClient => new IfClientCommand(),
            CommandList.IfServer => new IfServerCommand(),

            // Gamevar Conditions (Commands.cs)
            CommandList.IfVarE or CommandList.IfVarN or CommandList.IfVarG or CommandList.IfVarL
                or CommandList.IfVarAnd or CommandList.IfVarOr or CommandList.IfVarXor or CommandList.IfVarEither =>
                new IfVarCommand(command, GamevarConditionLookup.Map[command], cursor.ReadValue(), cursor.ReadValue()),
            CommandList.IfVarVarE or CommandList.IfVarVarN or CommandList.IfVarVarG or CommandList.IfVarVarL
                or CommandList.IfVarVarAnd or CommandList.IfVarVarOr or CommandList.IfVarVarXor or CommandList.IfVarVarEither =>
                new IfVarVarCommand(command, GamevarConditionLookup.Map[command], cursor.ReadValue(), cursor.ReadValue()),

            // Actor If (Commands.Conditionals.cs)
            CommandList.IfActor => new IfActorCommand(cursor.ReadValue()),
            CommandList.IfAction => new IfActionCommand(cursor.ReadValue()),
            CommandList.IfActionCount => new IfActionCountCommand(cursor.ReadValue()),
            CommandList.IfAi => new IfAiCommand(cursor.ReadValue()),
            CommandList.IfCount => new IfCountCommand(cursor.ReadValue()),
            CommandList.IfMove => new IfMoveCommand(cursor.ReadValue()),
            CommandList.IfSpawnedBy => new IfSpawnedByCommand(cursor.ReadValue()),
            CommandList.IfSpritePal => new IfSpritePalCommand(cursor.ReadValue()),
            CommandList.IfStrength => new IfStrengthCommand(cursor.ReadValue()),
            CommandList.IfHitWeapon => new IfHitWeaponCommand(),
            CommandList.IfWasWeapon => new IfWasWeaponCommand(cursor.ReadValue()),
            CommandList.IfDead => new IfDeadCommand(),
            CommandList.IfActorNotStayPut => new IfActorNotStayPutCommand(),

            // Surroundings If
            CommandList.IfAwayFromWall => new IfAwayFromWallCommand(),
            CommandList.IfBulletNear => new IfBulletNearCommand(),
            CommandList.IfCeilingDistL => new IfCeilingDistLCommand(cursor.ReadValue()),
            CommandList.IfFloorDistL => new IfFloorDistLCommand(cursor.ReadValue()),
            CommandList.IfGapZL => new IfGapZLCommand(cursor.ReadValue()),
            CommandList.IfSquished => new IfSquishedCommand(),
            CommandList.IfNotMoving => new IfNotMovingCommand(),
            CommandList.IfInWater => new IfInWaterCommand(),
            CommandList.IfOnWater => new IfOnWaterCommand(),
            CommandList.IfOutside => new IfOutsideCommand(),
            CommandList.IfInSpace => new IfInSpaceCommand(),
            CommandList.IfInOuterSpace => new IfInOuterSpaceCommand(),
            CommandList.IfRnd => new IfRndCommand(cursor.ReadValue()),

            // Player Interaction If
            CommandList.IfAngDiffL => new IfAngDiffLCommand(cursor.ReadValue()),
            CommandList.IfCanSee => new IfCanSeeCommand(),
            CommandList.IfCanSeeTarget => new IfCanSeeTargetCommand(),
            CommandList.IfCanShootTarget => new IfCanShootTargetCommand(),
            CommandList.IfHitSpace => new IfHitSpaceCommand(),

            // Player If
            CommandList.IfGotWeaponOnce => new IfGotWeaponOnceCommand(cursor.ReadValue()),
            CommandList.IfP => new IfPCommand(cursor.ReadAllContiguousValues()),
            CommandList.IfPDistG => new IfPDistGCommand(cursor.ReadValue()),
            CommandList.IfPDistL => new IfPDistLCommand(cursor.ReadValue()),
            CommandList.IfPHealthL => new IfPHealthLCommand(cursor.ReadValue()),
            CommandList.IfPInventory => new IfPInventoryCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.IfPlayersL => new IfPlayersLCommand(cursor.ReadValue()),

            // Audio/Cutscene If
            CommandList.IfSound => new IfSoundCommand(cursor.ReadValue()),
            CommandList.IfActorSound => new IfActorSoundCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.IfNoSounds => new IfNoSoundsCommand(),
            CommandList.IfCutscene => new IfCutsceneCommand(cursor.ReadInt()),

            _ => throw new FormatException($"No conditional-command mapping for '{command}'."),
        };
    }
}
