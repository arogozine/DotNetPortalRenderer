using RenderingEngine.Engine;
using SoftwareRendererModels;

namespace RenderingEngine.MapLoader;

public static class GameRenderStateLoader
{
    public static RenderableMap GenerateRenderableMap(FixedGameState fixedGameState)
    {
        Map map = fixedGameState.Map;

        RenderableSprite[] sprites = ParseSprites(map.Sprites);
        RenderableSector[] sectors = ParseMapSectors(map.Sectors);

        // Doom doesn't assign sectors to its sprites
        // but this engine does need that information to function
        if (fixedGameState.ResourceType == GameResourceType.Doom)
        {
            SpriteHelper.AssignSectors(sprites, sectors);
        }

        // Pre-Compute Bunches for Wall Handling
        WallHelper.AssignBunches(sectors);

        return new(sectors, sprites);
    }

    public static PlayerLocation GeneratePlayerLocation(FixedGameState fixedGameState)
    {
        return ParsePlayerStart(fixedGameState.Map.Player);
    }

    internal static PlayerLocation ParsePlayerStart(PlayerStart playerLocation)
    {
        return new PlayerLocation
        {
            Angle = playerLocation.ViewAngle,
            Sector = playerLocation.Sector,
            Where = playerLocation.Where
        };
    }

    internal static RenderableSprite[] ParseSprites(ReadOnlySpan<Sprite> sprites)
    {
        RenderableSprite[] renderableSprites = new RenderableSprite[sprites.Length];

        for (int i = 0; i < sprites.Length; i++)
        {
            renderableSprites[i] = ParseSprite(sprites[i]);
        }

        return renderableSprites;
    }

    internal static RenderableSprite ParseSprite(Sprite sprite)
    {
        return sprite switch
        {
            WallSprite wallSprite => new RenderableWallSprite { Sprite = wallSprite },
            FloorSprite floorSprite => new RenderableFloorSprite { Sprite = floorSprite },
            _ => new RenderableBasicSprite
            {
                Sprite = sprite
            },
        };
    }

    internal static RenderableSector[] ParseMapSectors(List<MapSector> mapSectors)
    {
        RenderableSector[] renderableMapSectors = new RenderableSector[mapSectors.Count];

        for (int i = 0; i < mapSectors.Count; i++)
        {
            renderableMapSectors[i] = ParseMapSector(mapSectors[i]);
        }

        return renderableMapSectors;
    }

    internal static RenderableSector ParseMapSector(MapSector x)
    {
        RenderableWall[] renderableWalls = new RenderableWall[x.Walls.Count];

        var sector = new RenderableSector
        {
            MapSector = x,
            Walls = renderableWalls
        };

        for (int i = 0; i < x.Walls.Count; i++)
        {
            Line v = x.Walls[i];

            renderableWalls[i] = new RenderableWall
            {
                Sector = sector,
                Line = v,
            };
        }

        return sector;

    }
}
