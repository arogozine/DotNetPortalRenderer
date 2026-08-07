namespace BuildAssetLoader.Con
{
    // Flat commands from Commands.Sectors.cs (Operating, Manipulation, Analysis, Discovery, Sorting).
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static Command? TryParseSectors(CommandList command, ConTreeCursor cursor) => command switch
        {
            CommandList.Operate => new OperateCommand(),
            CommandList.OperateActivators => new OperateActivatorsCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.OperateMasterSwitches => new OperateMasterSwitchesCommand(cursor.ReadValue()),
            CommandList.OperateRespawns => new OperateRespawnsCommand(cursor.ReadValue()),
            CommandList.OperateSectors => new OperateSectorsCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.ActivateBySector => new ActivateBySectorCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.Activate => new ActivateCommand(cursor.TryReadValue()),

            CommandList.DragPoint => new DragPointCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.MoveSector => new MoveSectorCommand(cursor.ReadValue()),
            CommandList.SectSetInterpolation => new SectSetInterpolationCommand(cursor.ReadValue()),
            CommandList.SectClearInterpolation => new SectClearInterpolationCommand(cursor.ReadValue()),

            CommandList.GetCeilZOfSlope => new GetCeilZOfSlopeCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.GetFlorZOfSlope => new GetFlorZOfSlopeCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.GetZRange => new GetZRangeCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.UpdateSector => new UpdateSectorCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.UpdateSectorZ => new UpdateSectorZCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.CheckActivatorMotion => new CheckActivatorMotionCommand(cursor.ReadValue()),
            CommandList.RotatePoint => new RotatePointCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.LineIntersect => new LineIntersectCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue()),
            CommandList.RayIntersect => new RayIntersectCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue()),
            CommandList.SectorOfWall => new SectorOfWallCommand(cursor.ReadValue(), cursor.ReadValue()),

            CommandList.FindNearActor => new FindNearActorCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.FindNearActorVar => new FindNearActorVarCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.FindNearActor3d => new FindNearActor3dCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.FindNearActor3dVar => new FindNearActor3dVarCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.FindNearSprite => new FindNearSpriteCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.FindNearSpriteVar => new FindNearSpriteVarCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.FindNearSprite3d => new FindNearSprite3dCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.FindNearSprite3dVar => new FindNearSprite3dVarCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            CommandList.FindNearActorZ => new FindNearActorZCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.FindNearActorZVar => new FindNearActorZVarCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.FindNearSpriteZ => new FindNearSpriteZCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.FindNearSpriteZVar => new FindNearSpriteZVarCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            CommandList.FindPlayer => new FindPlayerCommand(cursor.ReadValue()),
            CommandList.FindOtherPlayer => new FindOtherPlayerCommand(cursor.ReadValue()),

            CommandList.NearTag => new NearTagCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.Hitscan => new HitscanCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue()),

            CommandList.HeadSpriteSect => new HeadSpriteSectCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.HeadSpriteStat => new HeadSpriteStatCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.NextSpriteSect => new NextSpriteSectCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.NextSpriteStat => new NextSpriteStatCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.PrevSpriteSect => new PrevSpriteSectCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.PrevSpriteStat => new PrevSpriteStatCommand(cursor.ReadValue(), cursor.ReadValue()),

            _ => null,
        };
    }
}
