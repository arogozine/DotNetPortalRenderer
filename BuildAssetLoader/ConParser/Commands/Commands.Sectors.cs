namespace BuildAssetLoader.Con
{
    // ===== Sectors - Operating =====

    // operate — the current actor opens a nearby door.
    public sealed record OperateCommand() : Command(CommandList.operate);

    // operateactivators <lotag> <player id> — triggers ACTIVATOR/ACTIVATORLOCKED sprites by lotag.
    public sealed record OperateactivatorsCommand(string Lotag, string PlayerId) : Command(CommandList.operateactivators);

    // operatemasterswitches <lotag number>
    public sealed record OperatemasterswitchesCommand(string LotagNumber) : Command(CommandList.operatemasterswitches);

    // operaterespawns <lotag number>
    public sealed record OperaterespawnsCommand(string LotagNumber) : Command(CommandList.operaterespawns);

    // operatesectors <sector> <actor> — triggers elevator/lift sectors from actor code.
    public sealed record OperatesectorsCommand(string Sector, string Actor) : Command(CommandList.operatesectors);

    // activatebysector <sectNum> <spriteNum> — triggers ACTIVATOR sprites in a sector, or its tag effect if none found.
    public sealed record ActivatebysectorCommand(string SectNum, string SpriteNum) : Command(CommandList.activatebysector);

    // activate [<lotag>] — deprecated; same as operateactivators with a fixed player id of 0.
    public sealed record ActivateCommand(string? Lotag) : Command(CommandList.activate);

    // ===== Sectors - Manipulation =====

    // dragpoint <wallnum> <x> <y> — moves a wall vertex, like the map editor.
    public sealed record DragpointCommand(string Wallnum, string X, string Y) : Command(CommandList.dragpoint);

    // movesector <sprite ID> — moves a sector in x/y using the sprite's xvel/yvel (earthquakes, rotators, doors, escalators).
    public sealed record MovesectorCommand(string SpriteId) : Command(CommandList.movesector);

    // sectsetinterpolation <sectnum> — smooths a CON-driven sector's movement.
    public sealed record SectsetinterpolationCommand(string Sectnum) : Command(CommandList.sectsetinterpolation);

    // sectclearinterpolation <sectnum> — reverts a sector to the native 30Hz update rate.
    public sealed record SectclearinterpolationCommand(string Sectnum) : Command(CommandList.sectclearinterpolation);

    // ===== Sectors - Analysis =====

    // getceilzofslope <sectnum> <x> <y> <return>
    public sealed record GetceilzofslopeCommand(string Sectnum, string X, string Y, string ReturnVar) : Command(CommandList.getceilzofslope);

    // getflorzofslope <sectnum> <x> <y> <return>
    public sealed record GetflorzofslopeCommand(string Sectnum, string X, string Y, string ReturnVar) : Command(CommandList.getflorzofslope);

    // getzrange <x> <y> <z> <sector> <ceilingz> <ceilinghit> <floorz> <floorhit> <walldist> <clipmask>
    public sealed record GetzrangeCommand(
        string X, string Y, string Z, string Sector,
        string CeilingZ, string CeilingHit, string FloorZ, string FloorHit, string WallDist, string ClipMask)
        : Command(CommandList.getzrange);

    // updatesector <in_x> <in_y> <in_out_sectnum> — sector containing (x, y); in_out_sectnum must be pre-seeded.
    public sealed record UpdatesectorCommand(string InX, string InY, string InOutSectnum) : Command(CommandList.updatesector);

    // updatesectorz <in_x> <in_y> <in_z> <in_out_sectnum> — sector containing (x, y, z).
    public sealed record UpdatesectorzCommand(string InX, string InY, string InZ, string InOutSectnum) : Command(CommandList.updatesectorz);

    // checkactivatormotion <lotag> — sets RETURN to 1 if an activator with the given lotag is in motion.
    public sealed record CheckactivatormotionCommand(string Lotag) : Command(CommandList.checkactivatormotion);

    // rotatepoint <xpivot> <ypivot> <x> <y> <ang> <xreturnvar> <yreturnvar>
    public sealed record RotatepointCommand(
        string Xpivot, string Ypivot, string X, string Y, string Ang, string XReturnVar, string YReturnVar)
        : Command(CommandList.rotatepoint);

    // lineintersect <ox> <oy> <oz> <dx> <dy> <dz> <x1> <y1> <x2> <y2> <intx> <inty> <intz> <ret>
    public sealed record LineintersectCommand(
        string Ox, string Oy, string Oz, string Dx, string Dy, string Dz,
        string X1, string Y1, string X2, string Y2, string Intx, string Inty, string Intz, string Ret)
        : Command(CommandList.lineintersect);

    // rayintersect <x> <y> <z> <vx> <vy> <vz> <x1> <y1> <x2> <y2> <intx> <inty> <intz> <ret>
    public sealed record RayintersectCommand(
        string X, string Y, string Z, string Vx, string Vy, string Vz,
        string X1, string Y1, string X2, string Y2, string Intx, string Inty, string Intz, string Ret)
        : Command(CommandList.rayintersect);

    // sectorofwall <returnvar> <wall ID>
    public sealed record SectorofwallCommand(string ReturnVar, string WallId) : Command(CommandList.sectorofwall);

    // ===== Discovery - Searching =====

    // findnearactor/findnearactorvar/findnearactor3d/findnearactor3dvar/findnearsprite/findnearspritevar/findnearsprite3d/findnearsprite3dvar
    // <tile number> <distance> <gamevar> — cylinder (normal) or sphere (3d) search radius.
    public record BaseFindnearCommand(CommandList Start, string TileNumber, string Distance, string Gamevar) : Command(Start);

    public sealed record FindnearactorCommand(string TileNumber, string Distance, string Gamevar)
        : BaseFindnearCommand(CommandList.findnearactor, TileNumber, Distance, Gamevar);
    public sealed record FindnearactorvarCommand(string TileNumber, string Distance, string Gamevar)
        : BaseFindnearCommand(CommandList.findnearactorvar, TileNumber, Distance, Gamevar);
    public sealed record Findnearactor3dCommand(string TileNumber, string Distance, string Gamevar)
        : BaseFindnearCommand(CommandList.findnearactor3d, TileNumber, Distance, Gamevar);
    public sealed record Findnearactor3dvarCommand(string TileNumber, string Distance, string Gamevar)
        : BaseFindnearCommand(CommandList.findnearactor3dvar, TileNumber, Distance, Gamevar);
    public sealed record FindnearspriteCommand(string TileNumber, string Distance, string Gamevar)
        : BaseFindnearCommand(CommandList.findnearsprite, TileNumber, Distance, Gamevar);
    public sealed record FindnearspritevarCommand(string TileNumber, string Distance, string Gamevar)
        : BaseFindnearCommand(CommandList.findnearspritevar, TileNumber, Distance, Gamevar);
    public sealed record Findnearsprite3dCommand(string TileNumber, string Distance, string Gamevar)
        : BaseFindnearCommand(CommandList.findnearsprite3d, TileNumber, Distance, Gamevar);
    public sealed record Findnearsprite3dvarCommand(string TileNumber, string Distance, string Gamevar)
        : BaseFindnearCommand(CommandList.findnearsprite3dvar, TileNumber, Distance, Gamevar);

    // findnearactorz/findnearactorzvar/findnearspritez/findnearspritezvar
    // <tile number> <xydistance> <zdistance> <gamevar> — finite cylinder search.
    public record BaseFindnearzCommand(CommandList Start, string TileNumber, string XyDistance, string ZDistance, string Gamevar) : Command(Start);

    public sealed record FindnearactorzCommand(string TileNumber, string XyDistance, string ZDistance, string Gamevar)
        : BaseFindnearzCommand(CommandList.findnearactorz, TileNumber, XyDistance, ZDistance, Gamevar);
    public sealed record FindnearactorzvarCommand(string TileNumber, string XyDistance, string ZDistance, string Gamevar)
        : BaseFindnearzCommand(CommandList.findnearactorzvar, TileNumber, XyDistance, ZDistance, Gamevar);
    public sealed record FindnearspritezCommand(string TileNumber, string XyDistance, string ZDistance, string Gamevar)
        : BaseFindnearzCommand(CommandList.findnearspritez, TileNumber, XyDistance, ZDistance, Gamevar);
    public sealed record FindnearspritezvarCommand(string TileNumber, string XyDistance, string ZDistance, string Gamevar)
        : BaseFindnearzCommand(CommandList.findnearspritezvar, TileNumber, XyDistance, ZDistance, Gamevar);

    // findplayer/findotherplayer <gamevar> — distance to nearest player into <gamevar>, id into RETURN.
    public record BaseFindplayerCommand(CommandList Start, string Gamevar) : Command(Start);

    public sealed record FindplayerCommand(string Gamevar) : BaseFindplayerCommand(CommandList.findplayer, Gamevar);
    public sealed record FindotherplayerCommand(string Gamevar) : BaseFindplayerCommand(CommandList.findotherplayer, Gamevar);

    // neartag <x> <y> <z> <sect> <ang> <nearTagSector> <nearTagWall> <nearTagSprite> <nearTagHitDist> <nearTagRange> <tagSearch>
    public sealed record NeartagCommand(
        string X, string Y, string Z, string Sect, string Ang,
        string NearTagSector, string NearTagWall, string NearTagSprite, string NearTagHitDist, string NearTagRange, string TagSearch)
        : Command(CommandList.neartag);

    // hitscan <x1> <y1> <z1> <sect1> <cos of ang> <sin of ang> <zvel> <hit sector> <hit wall> <hit sprite> <hit x> <hit y> <hit z> <clip mask>
    public sealed record HitscanCommand(
        string X1, string Y1, string Z1, string Sect1, string CosAng, string SinAng, string Zvel,
        string HitSectorVar, string HitWallVar, string HitSpriteVar, string HitXVar, string HitYVar, string HitZVar, string ClipMask)
        : Command(CommandList.hitscan);

    // ===== Sorting =====

    // headspritesect <sprite> <sect> — first sprite id in a sector's linked list.
    public sealed record HeadspritesectCommand(string Sprite, string Sect) : Command(CommandList.headspritesect);

    // headspritestat <sprite> <statnum> — first sprite id in a statnum's linked list.
    public sealed record HeadspritestatCommand(string Sprite, string Statnum) : Command(CommandList.headspritestat);

    // nextspritesect <NextSpriteID> <CurrentSpriteID>
    public sealed record NextspritesectCommand(string NextSpriteId, string CurrentSpriteId) : Command(CommandList.nextspritesect);

    // nextspritestat <NextSpriteID> <CurrentSpriteID>
    public sealed record NextspritestatCommand(string NextSpriteId, string CurrentSpriteId) : Command(CommandList.nextspritestat);

    // prevspritesect <PrevSpriteID> <CurrentSpriteID>
    public sealed record PrevspritesectCommand(string PrevSpriteId, string CurrentSpriteId) : Command(CommandList.prevspritesect);

    // prevspritestat <PrevSpriteID> <CurrentSpriteID>
    public sealed record PrevspritestatCommand(string PrevSpriteId, string CurrentSpriteId) : Command(CommandList.prevspritestat);
}
