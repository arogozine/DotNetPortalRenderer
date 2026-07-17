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

        PrecomputeTextureProperties(sectors);

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
                Line = v
            };
        }

        return sector;

    }


    internal static void PrecomputeTextureProperties(ReadOnlySpan<RenderableSector> sectors)
    {
        const float maxScale = 1f;

        for (int s = 0; s < sectors.Length; s++)
        {
            RenderableSector sector = sectors[s];
            ReadOnlySpan<RenderableWall> walls = sector.Walls;

            for (int w = 0; w < walls.Length; w++)
            {
                RenderableWall wall = walls[w];

                if (!wall.IsPortal)
                {
                    if (wall.MiddleTexture is { } texture && texture.YOffset == 0)
                    {
                        int textureHeight = texture.Height;

                        if (texture.YScale is { } yScale)
                        {
                            yScale = (sector.Ceil - sector.Floor) * yScale;

                            texture.YUntiled = yScale <= maxScale;
                        }
                        else
                        {
                            texture.YUntiled = textureHeight <= (sector.Ceil - sector.Floor);
                        }
                    }

                    continue;
                }

                Debug.Assert(wall.Neighbor != null);
                RenderableSector neighborSector = sectors[wall.Neighbor.Value];
                bool sloped = neighborSector is not null && (sector.Settings.Sloped || neighborSector.Settings.Sloped);

                if (sloped)
                {
                    continue;
                }

                (_, float ceilingOffset, float floorOffset) = CalculatePortalOffsets(sector.Floor, sector.Ceil, neighborSector!.Floor, neighborSector.Ceil);

                if (wall.UpperTexture is { } upperTexture && (upperTexture.YOffset == 0 || upperTexture.YOffset == ceilingOffset))
                {
                    ceilingOffset = -ceilingOffset;

                    int textureHeight = upperTexture.Height;

                    if (upperTexture.YScale is float yScale)
                    {
                        yScale = ceilingOffset * yScale;

                        upperTexture.YUntiled = yScale <= maxScale;
                    }
                    else
                    {
                        upperTexture.YUntiled = textureHeight <= ceilingOffset;
                    }

                }

                if (wall.MiddleTexture is { } middleTexture && middleTexture.YOffset == 0)
                {

                }

                if (wall.LowerTexture is { } lowerTexture && lowerTexture.YOffset == 0)
                {
                    int textureHeight = lowerTexture.Height;

                    if (lowerTexture.YScale is float yScale)
                    {
                        yScale = floorOffset * yScale;

                        lowerTexture.YUntiled = yScale <= maxScale;
                    }
                    else
                    {
                        lowerTexture.YUntiled = textureHeight <= floorOffset;
                    }
                }
            }
        }
    }

    private static (float SectorHeight, float CeilingOffset, float FloorOffset) CalculatePortalOffsets(float floorA, float ceilA, float floorB, float ceilB)
    {
        float sectorHeight = ceilA - floorA;
        float floorOffset = floorB - floorA;
        float ceilOffset = ceilB - ceilA;

        if (floorOffset < 0f)
        {
            floorOffset = 0f;
        }

        if (ceilOffset > 0f)
        {
            ceilOffset = 0f;
        }

        // don't draw beyond the bounds
        if (ceilOffset < -sectorHeight)
        {
            ceilOffset = -sectorHeight;
        }

        if (floorOffset > sectorHeight)
        {
            floorOffset = sectorHeight;
        }

        return (sectorHeight, ceilOffset, floorOffset);
    }
}
