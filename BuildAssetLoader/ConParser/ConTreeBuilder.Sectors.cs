namespace BuildAssetLoader.Con
{
    // Flat commands from Commands.Sectors.cs (Operating, Manipulation, Analysis, Discovery, Sorting).
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static Command? TryParseSectors(CommandList command, ConTreeCursor cursor) => command switch
        {
            CommandList.operate => new OperateCommand(),
            CommandList.operateactivators => new OperateactivatorsCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.operatemasterswitches => new OperatemasterswitchesCommand(cursor.ReadValue()),
            CommandList.operaterespawns => new OperaterespawnsCommand(cursor.ReadValue()),
            CommandList.operatesectors => new OperatesectorsCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.activatebysector => new ActivatebysectorCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.activate => new ActivateCommand(cursor.TryReadValue()),

            CommandList.dragpoint => new DragpointCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.movesector => new MovesectorCommand(cursor.ReadValue()),
            CommandList.sectsetinterpolation => new SectsetinterpolationCommand(cursor.ReadValue()),
            CommandList.sectclearinterpolation => new SectclearinterpolationCommand(cursor.ReadValue()),

            CommandList.getceilzofslope => new GetceilzofslopeCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.getflorzofslope => new GetflorzofslopeCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.getzrange => new GetzrangeCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.updatesector => new UpdatesectorCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.updatesectorz => new UpdatesectorzCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.checkactivatormotion => new CheckactivatormotionCommand(cursor.ReadValue()),
            CommandList.rotatepoint => new RotatepointCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.lineintersect => new LineintersectCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue()),
            CommandList.rayintersect => new RayintersectCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue()),
            CommandList.sectorofwall => new SectorofwallCommand(cursor.ReadValue(), cursor.ReadValue()),

            CommandList.findnearactor => new FindnearactorCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.findnearactorvar => new FindnearactorvarCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.findnearactor3d => new Findnearactor3dCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.findnearactor3dvar => new Findnearactor3dvarCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.findnearsprite => new FindnearspriteCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.findnearspritevar => new FindnearspritevarCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.findnearsprite3d => new Findnearsprite3dCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.findnearsprite3dvar => new Findnearsprite3dvarCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            CommandList.findnearactorz => new FindnearactorzCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.findnearactorzvar => new FindnearactorzvarCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.findnearspritez => new FindnearspritezCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.findnearspritezvar => new FindnearspritezvarCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            CommandList.findplayer => new FindplayerCommand(cursor.ReadValue()),
            CommandList.findotherplayer => new FindotherplayerCommand(cursor.ReadValue()),

            CommandList.neartag => new NeartagCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.hitscan => new HitscanCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue()),

            CommandList.headspritesect => new HeadspritesectCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.headspritestat => new HeadspritestatCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.nextspritesect => new NextspritesectCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.nextspritestat => new NextspritestatCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.prevspritesect => new PrevspritesectCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.prevspritestat => new PrevspritestatCommand(cursor.ReadValue(), cursor.ReadValue()),

            _ => null,
        };
    }
}
