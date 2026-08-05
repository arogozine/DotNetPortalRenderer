using BuildAssetLoader;
using BuildAssetLoader.Con;
using BuildAssetLoader.Map;
using BuildAssetLoader.Texture;
using RenderingEngine.Engine;
using RenderingEngine.Tooling;
using SoftwareRendererModels;
using System.Numerics;

namespace RenderingEngine.MapLoader;

internal static class GrpReader
{
    public static Map LoadBuildMap(GrpFile grp, string mapName, Dictionary<int, SpriteAngleRotation[]> spriteToAngleFrames)
    {
        var maps = BuildFileParser.ExtractMapFiles(grp);

        MapFile? map = maps.FirstOrDefault(x => x.MapName.Equals(mapName, StringComparison.InvariantCultureIgnoreCase));

        if (map is null)
        {
            AsyncLogger.Default.AddLog(LogSeverity.Error, $"Map '{mapName}' not found in GRP file");
            AsyncLogger.Default.WaitSync();
            Environment.Exit(1);
        }

        return ExtractBuildMap(map, spriteToAngleFrames);
    }

    public static void ExtractAllTextures(GrpFile grp, PaletteFile paletteFile, LookupFile lookupFile)
    {
        List<ArtFile> artFiles = BuildFileParser.ExtractArtFiles(grp);
        Dictionary<string, TextureInfo> textures = ExtractTextures(artFiles);

        ReadOnlySpan<BGRA> pal = ToBGRA(MemoryMarshal.Cast<byte, RGB>(paletteFile.Palette));

        // Shades for Palette 0
        for (int i = 0; i < paletteFile.PalLookups.Length; i++)
        {
            Span<byte> lookup = paletteFile.PalLookups[i];

            BGRA[] palette = new BGRA[lookup.Length];

            for (int j = 0; j < lookup.Length; j++)
            {
                byte palIndex = lookup[j];

                // 255th index is used for transparency
                if (palIndex == byte.MaxValue)
                {
                    palette[j] = BGRA.Transparent;
                }
                else
                {
                    palette[j] = pal[palIndex];
                }
            }

            TextureCache.AddPallette(0, i, palette);
        }

        // Lookup Palettes
        for (int s = 0; s < lookupFile.PaletteSwapTables.Length; s++)
        {
            // full black
            if (s == 3)
            {
                Span<byte> lookup = paletteFile.PalLookups[0];

                BGRA[] palette = new BGRA[lookup.Length];

                for (int j = 0; j < lookup.Length; j++)
                {
                    byte palIndex = lookup[j];

                    // 255th index is used for transparency
                    if (palIndex == byte.MaxValue)
                    {
                        palette[j] = BGRA.Transparent;
                    }
                    else
                    {
                        palette[j] = BGRA.Black;
                    }
                }

                for (int i = 0; i < paletteFile.PalLookups.Length; i++)
                {
                    TextureCache.AddPallette(s + 1, i, palette);
                }

                continue;
            }

            // Shades
            for (int i = 0; i < paletteFile.PalLookups.Length; i++)
            {
                Span<byte> swapTable = lookupFile.PaletteSwapTables[s];
                Span<byte> lookup0 = paletteFile.PalLookups[i];

                Span<byte> lookup = new byte[swapTable.Length];

                for (int x = 0; x < lookup.Length; x++)
                {
                    lookup[x] = swapTable[lookup0[x]];
                }

                BGRA[] palette = new BGRA[lookup.Length];

                for (int j = 0; j < lookup.Length; j++)
                {
                    byte palIndex = lookup[j];

                    // 255th index is used for transparency
                    if (palIndex == byte.MaxValue)
                    {
                        palette[j] = BGRA.Transparent;
                    }
                    else
                    {
                        palette[j] = pal[palIndex];
                    }
                }

                TextureCache.AddPallette(s + 1, i, palette);
            }
        }

        foreach ((string name, var info) in textures)
        {
            TextureCache.Add(name, new BuildTexture(name, info.Width, info.Height, info.Data));
        }
    }

    public static Dictionary<int, SpriteAngleRotation[]> ExtractSpriteAngleInfo(string defsConPath, string gameConPath)
    {
        List<ConToken> defsTokens = ConParser.Parse(File.ReadAllText(defsConPath));
        List<ConToken> gameConTokens = ConParser.Parse(File.ReadAllText(gameConPath));

        List<Command> commands = ParseOutCommands(defsTokens);
        commands.AddRange(ParseOutCommands(gameConTokens));

        Dictionary<string, DefineCommand> defines = commands
            .OfType<DefineCommand>()
            .ToDictionary(static x => x.Name, static x => x);

        var actors = commands
            .OfType<BaseActorCommand>()
            .ToDictionary(static x => x.PicNum, static x => x);

        var aiCommandToAction = commands
            .OfType<AiCommand>()
            .ToDictionary(static x => x.Name, static x => x.Action);

        var actions = commands.OfType<ActionCommand>()
            .ToDictionary(static x => x.Name, static x => x);

        Dictionary<int, SpriteAngleRotation[]> spriteToActions = [];

        foreach (var actor in actors.Values)
        {
            string? action = actor.Action;
            string picNum = actor.PicNum;
            string cActorPicNum = picNum;

            if (action == null)
            {
                if (actor.Body.Count == 0)
                {
                    continue;
                }

                if (TryGet(actor, out ActionCommand? actionCommandBody))
                {
                    action = actionCommandBody.Name;
                }
                else if (TryGet(actor, out AiCommand? aiCommand) && aiCommandToAction.TryGetValue(aiCommand.Name, out string? actionName))
                {
                    action = actionName;
                }
                else
                {
                    continue;
                }
            }

            Debug.Assert(action != null);

            if (!actions.TryGetValue(action, out ActionCommand? actionCommand))
            {
                continue;
            }

            if (TryGet(actor, out CActorCommand? cActor) && actors.TryGetValue(cActor.Name, out BaseActorCommand? command))
            {
                cActorPicNum = command.PicNum;
            }

            if (!defines.TryGetValue(picNum, out DefineCommand? defineCommand))
            {
                continue;
            }

            if (!defines.TryGetValue(cActorPicNum, out DefineCommand? cActorDefineCommand))
            {
                continue;
            }

            int defineNumber = int.Parse(defineCommand.Value);
            int cActorDefineNumber = int.Parse(cActorDefineCommand.Value);

            Debug.Assert(TextureCache.HasTexture(ToTile(defineNumber)));
            Debug.Assert(TextureCache.HasTexture(ToTile(cActorDefineNumber)));

            if (DetermineSpriteAngles(cActorDefineNumber, actionCommand, out SpriteAngleRotation[]? spriteAngleInfo))
            {
                spriteToActions.Add(defineNumber, spriteAngleInfo);
            }
        }

        return spriteToActions;

        static bool TryGet<C>(Structure action, [NotNullWhen(true)] out C? command)
            where C : Command
        {
            if (action.Body.Count > 0)
            {
                command = (C?)action.Body.FirstOrDefault(static (x) => x is C);
                return command != null;
            }

            command = null;
            return false;
        }
    }

    internal record class SpriteAngleRotation(int Sprite, bool Flipped, float? Angle);

    private static bool DetermineSpriteAngles(int startSprite, ActionCommand actionCommand,
        [NotNullWhen(true)] out SpriteAngleRotation[]? angles)
    {
        angles = null;

        if (actionCommand.ViewType is null)
        {
            return false;
        }

        if (actionCommand.Startframe is { } startFrame)
        {
            startSprite += startFrame;
        }

        switch (actionCommand.ViewType)
        {
            case 0:
                return false;
            // The sprite will appear the same regardless of the angle at which it is viewed.
            case 1:
                return false;
            // The sprite will have 8 angles built from only 2 art tiles.
            // A new frame is drawn every 45 degrees in a clockwise pattern beginning with the front of the sprite.
            case 2:
                {
                    Span<byte> spriteNum = [1, 2, 1, 2, 1, 2, 1, 2];

                    angles = new SpriteAngleRotation[spriteNum.Length];
                    float angle = 0f;

                    for (int i = 0; i < spriteNum.Length; i++, angle += (MathF.PI / 4))
                    {
                        int sprite = spriteNum[i] + startSprite - 1;

                        if (!TextureCache.HasTexture(ToTile(sprite)))
                        {
                            sprite += 2;
                        }

                        Debug.Assert(TextureCache.HasTexture(ToTile(sprite)));

                        angles[i] = new SpriteAngleRotation(sprite, false, angle);
                    }
                    return true;
                }
            // The sprite will have 16 angles built from only 4 art tiles.
            // A new frame is drawn every 22.5 degrees in a clockwise pattern beginning with the front of the sprite.
            case 3:
            case 4:
                {
                    Span<byte> spriteNum = [1, 2, 3, 4, 4, 3, 2, 1, 1, 2, 3, 4, 4, 3, 2, 1];

                    angles = new SpriteAngleRotation[spriteNum.Length];
                    float angle = 0f;
                    for (int i = 0; i < spriteNum.Length; i++, angle += (MathF.PI / 8))
                    {
                        bool mirrored = i < 4 || (i > 8 && i < 12);
                        int sprite = spriteNum[i] + startSprite - 1;

                        if (!TextureCache.HasTexture(ToTile(sprite)))
                        {
                            sprite += 4;
                        }

                        Debug.Assert(TextureCache.HasTexture(ToTile(sprite)));

                        angles[i] = new SpriteAngleRotation(sprite, mirrored, angle);
                    }

                    return true;
                }
            // The sprite will have 8 angles constructed from 5 art tiles, three of which are mirrored.
            // A new frame is drawn every 45 degrees in a clockwise pattern beginning with the front of the sprite.
            case 5:
                {
                    Span<byte> spriteNum = [1, 2, 3, 4, 5, 4, 3, 2];

                    angles = new SpriteAngleRotation[spriteNum.Length];
                    float angle = 0f;

                    for (int i = 0; i < spriteNum.Length; i++, angle += (MathF.PI / 4))
                    {
                        bool mirrored = i > 4;
                        int sprite = spriteNum[i] + startSprite - 1;

                        if (!TextureCache.HasTexture(ToTile(sprite)))
                        {
                            sprite += 5;
                        }

                        // can't figure this out
                        if (!TextureCache.HasTexture(ToTile(sprite)))
                        {
                            return false;
                        }

                        Debug.Assert(TextureCache.HasTexture(ToTile(sprite)));

                        angles[i] = new SpriteAngleRotation(sprite, mirrored, angle);
                    }

                    return true;
                }
            //  The sprite will have 12 angles constructed from 7 art tiles, five of which are mirrored. A new frame is drawn every 30 degrees in a clockwise pattern beginning with the front of the sprite.
            case 7:
                {
                    Span<byte> spriteNum = [1, 2, 3, 4, 5, 6, 7, 6, 5, 4, 3, 2];

                    angles = new SpriteAngleRotation[spriteNum.Length];
                    float angle = 0f;

                    for (int i = 0; i < spriteNum.Length; i++, angle += (MathF.PI / 6))
                    {
                        bool mirrored = i > 5;
                        int sprite = spriteNum[i] + startSprite - 1;

                        if (!TextureCache.HasTexture(ToTile(sprite)))
                        {
                            sprite += 7;
                        }

                        Debug.Assert(TextureCache.HasTexture(ToTile(sprite)));

                        angles[i] = new SpriteAngleRotation(sprite, mirrored, angle);
                    }

                    return true;
                }
            default:
                throw new NotImplementedException();

        }
    }

    private static List<Command> ParseOutCommands(List<ConToken> tokens)
    {
        // Dictionary<string, string>

        List<Command> commands = [];

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            if (token is CommandToken commandToken)
            {
                switch (commandToken.Command)
                {
                    case CommandList.ai:
                        {
                            string name = ((ValueToken)tokens[++i]).Value;
                            ValueToken? action, move = null;

                            bool found =
                                GetNextIf(ref i, out action) &&
                                GetNextIf(ref i, out move);

                            Debug.Assert(found);
                            commands.Add(new AiCommand(name, action?.Value, move?.Value, []));
                        }
                        break;
                    case CommandList.define:
                        {
                            string name = ((ValueToken)tokens[++i]).Value;
                            string number = ((ValueToken)tokens[++i]).Value;
                            commands.Add(new DefineCommand(name, number));
                        }
                        break;
                    case CommandList.actor:
                        {
                            string picNum = ((ValueToken)tokens[++i]).Value;

                            ValueToken? strength, action = null, move = null;

                            _ = GetNextIf(ref i, out strength) &&
                                GetNextIf(ref i, out action) &&
                                GetNextIf(ref i, out move);

                            var actor = new ActorCommand(picNum, strength?.Value, action?.Value, move?.Value, []);

                            if (TryGetAction(i, out ActionCommand? actionCommand))
                            {
                                actor.Body.Add(actionCommand);
                            }

                            if (TryGetCActor(i, out CActorCommand? cActor))
                            {
                                actor.Body.Add(cActor);
                            }

                            if (TryGetAICommandFromBody(i, out AiCommand? ai))
                            {
                                actor.Body.Add(ai);
                            }

                            commands.Add(actor);

                            SkipUntil(ref i, CommandList.enda);
                        }
                        break;
                    case CommandList.useractor:
                        {
                            string type = ((ValueToken)tokens[++i]).Value;
                            string picNum = ((ValueToken)tokens[++i]).Value;

                            ValueToken? strength, action = null, move = null;

                            _ = GetNextIf(ref i, out strength) &&
                                GetNextIf(ref i, out action) &&
                                GetNextIf(ref i, out move);

                            var userActor = new UserActorCommand(type, picNum, strength?.Value, action?.Value, move?.Value, []);

                            if (TryGetAction(i, out ActionCommand? actionCommand))
                            {
                                userActor.Body.Add(actionCommand);
                            }

                            if (TryGetCActor(i, out CActorCommand? cActor))
                            {
                                userActor.Body.Add(cActor);
                            }

                            if (TryGetAICommandFromBody(i, out AiCommand? ai))
                            {
                                userActor.Body.Add(ai);
                            }

                            commands.Add(userActor);

                            SkipUntil(ref i, CommandList.enda);
                        }
                        break;
                    case CommandList.action:
                        {
                            string name = ((ValueToken)tokens[++i]).Value;

                            int? startFrame, frames = null, viewType = null, incValue = null, delay = null;

                            _ = GetNext(ref i, out startFrame) &&
                                GetNext(ref i, out frames) &&
                                GetNext(ref i, out viewType) &&
                                GetNext(ref i, out incValue) &&
                                GetNext(ref i, out delay);

                            commands.Add(new ActionCommand(name, startFrame, frames, viewType, incValue, delay));
                        }
                        break;
                    case CommandList.state:
                        {
                            SkipUntil(ref i, CommandList.ends);
                        }
                        break;
                }
            }
        }

        return commands;

        void SkipUntil(ref int i, CommandList command)
        {
            ConToken token;
            do
            {
                i++;
                token = tokens[i];
            }
            while (token is not CommandToken commandToken || commandToken.Command != command);
        }

        bool GetNext<T>(ref int i, [NotNullWhen(true)] out T? value)
            where T : struct, IParsable<T>
        {
            if (GetNextIf(ref i, out ValueToken? valueToken))
            {
                value = T.Parse(valueToken.Value, null);
                return true;
            }

            value = default!;
            return false;
        }

        bool GetNextIf<T>(ref int i, [NotNullWhen(true)] out T? value)
            where T : ConToken
        {
            ConToken token = tokens[i + 1];

            value = token as T;

            if (value != null)
            {
                i++;
                return true;
            }

            return false;
        }

        bool TryGetAction(int i, [NotNullWhen(true)] out ActionCommand? actionCommand)
        {
            int depth = 0;

            for (i++; i < tokens.Count; i++)
            {
                ConToken token = tokens[i];

                if (token.ConTokenType == ConTokenType.BlockStart)
                {
                    depth++;
                    continue;
                }

                if (token.ConTokenType == ConTokenType.BlockEnd)
                {
                    depth--;
                    continue;
                }

                if (depth != 0)
                {
                    continue;
                }

                if (token is CommandToken commandToken)
                {
                    if (commandToken.Command == CommandList.enda)
                    {
                        actionCommand = null;
                        return false;
                    }

                    if (commandToken.Command == CommandList.action)
                    {
                        if (GetNextIf(ref i, out ValueToken? name))
                        {
                            actionCommand = new ActionCommand(name.Value, null, null, null, null, null);
                            return true;
                        }
                    }
                }
            }

            actionCommand = null;
            return false;
        }

        bool TryGetCActor(int i, [NotNullWhen(true)] out CActorCommand? cActor)
        {
            int depth = 0;

            for (i++; i < tokens.Count; i++)
            {
                ConToken token = tokens[i];

                if (token.ConTokenType == ConTokenType.BlockStart)
                {
                    depth++;
                    continue;
                }

                if (token.ConTokenType == ConTokenType.BlockEnd)
                {
                    depth--;
                    continue;
                }

                if (depth != 0)
                {
                    continue;
                }

                if (token is CommandToken commandToken)
                {
                    if (commandToken.Command == CommandList.enda)
                    {
                        cActor = null;
                        return false;
                    }

                    if (commandToken.Command == CommandList.cactor)
                    {
                        if (GetNextIf(ref i, out ValueToken? name))
                        {
                            cActor = new CActorCommand(name.Value);
                            return true;
                        }
                    }
                }
            }

            cActor = null;
            return false;
        }

        bool TryGetAICommandFromBody(int i, [NotNullWhen(true)] out AiCommand? aiCommand)
        {
            for (i++; i < tokens.Count; i++)
            {
                ConToken token = tokens[i];

                if (token is CommandToken commandToken)
                {
                    if (commandToken.Command == CommandList.enda)
                    {
                        aiCommand = null;
                        return false;
                    }

                    if (commandToken.Command == CommandList.ai)
                    {
                        if (GetNextIf(ref i, out ValueToken? name))
                        {
                            aiCommand = new AiCommand(name.Value, null, null, null);
                            return true;
                        }
                    }
                }
            }

            aiCommand = null;
            return false;
        }
    }

    private static (TextureRenderingOptions, int XScale, int YScale) ToTextureRenderingOptions(Stat stat)
    {
        int xScale = 1;
        int yScale = 1;

        TextureRenderingOptions options = default;

        if (stat.HasFlag(Stat.Parallaxing))
        {
            options |= TextureRenderingOptions.Skybox;
        }

        if (stat.HasFlag(Stat.XFlip))
        {
            options |= TextureRenderingOptions.FlipX;
        }

        if (stat.HasFlag(Stat.YFlip))
        {
            options |= TextureRenderingOptions.FlipY;
        }

        if (stat.HasFlag(Stat.SwapXy))
        {
            options |= TextureRenderingOptions.SwapXY;
        }

        if (!stat.HasFlag(Stat.DoubleSmooshiness))
        {
            xScale = 2;
            yScale = 2;
        }

        if (stat.HasFlag(Stat.AlignTexture))
        {
            options |= TextureRenderingOptions.AlignWithFirstWall;
        }

        if (stat.HasFlag(Stat.Sloped))
        {
            options |= TextureRenderingOptions.Sloped;
        }

        return (options, xScale, yScale);
    }

    private static TextureRenderingOptions ToTextureRenderingOptions(WallCStat stat, bool bottomsSwapped)
    {
        TextureRenderingOptions options = default;

        if (stat.HasFlag(WallCStat.AlignPictureOnBottom))
        {
            options |= TextureRenderingOptions.FromSectorBottom;
        }
        else
        {
            options |= TextureRenderingOptions.FromSectorTop;
        }

        if (stat.HasFlag(WallCStat.XFlipped))
        {
            options |= TextureRenderingOptions.MirrorX;
        }

        if (stat.HasFlag(WallCStat.YFlipped))
        {
            options |= TextureRenderingOptions.FlipY;
        }

        if (bottomsSwapped)
        {
            options |= TextureRenderingOptions.FromLower;
        }

        if (stat.HasFlag(WallCStat.Rotate90))
        {
            throw new NotImplementedException();
        }

        return options;
    }

    private static Map ExtractBuildMap(MapFile mapFile, Dictionary<int, SpriteAngleRotation[]> spriteToAngleFrames)
    {
        StartingPosition startingPosition = mapFile.StartingPosition;
        Span<SectorType> grpSectors = mapFile.Sectors;
        Span<WallType> walls = mapFile.Walls;
        Span<SpriteType> sprites = mapFile.Sprites;

        int ij = 0;

        var sectors = new List<MapSector>(grpSectors.Length);

        for (int i = 0; i < grpSectors.Length; i++)
        {
            ref SectorType sector = ref grpSectors[i];

            MapSector mapSector = ParseSectorType(i, in sector);

            int wallStart = sector.WallPtr;
            int wallEnd = wallStart + sector.WallNum;

            for (int j = wallStart; j < wallEnd; j++)
            {
                ref WallType wall = ref walls[j];
                ref WallType point2Wall = ref walls[wall.Point2];
                ref WallType nextWall = ref wall;

                // If the wall has cstat 2 applied to it (CSTAT_WALL_BOTTOM_SWAP) than the bottom half's attributes are applied to the current wall's nextwall
                // https://wiki.eduke32.com/wiki/Cstat_(wall)
                if (wall.CStat.HasFlag(WallCStat.BottomsInvisibleWallsSwapped) && wall.NextWall != -1)
                {
                    nextWall = ref walls[wall.NextWall];
                }

                var line = new Line
                {
                    Id = ij,
                    PointA = new LineVector(j, GetPoint(in wall)),
                    PointB = new LineVector(wall.Point2, GetPoint(in point2Wall)),
                    SectorTo = wall.NextSector == -1 ? null : wall.NextSector,
                    UpperTexture = GetTextureInfo(in wall, in wall, false),
                    MiddleTexture = GetTextureInfo(in wall, in wall, true),
                    LowerTexture = GetTextureInfo(in wall, in nextWall, false),
                    UpperShade = wall.Shade,
                    LowerShade = nextWall.Shade,
                    Traversable = !wall.CStat.HasFlag(WallCStat.BlockingWallClipmove)
                };

                ij++;

                mapSector.Walls.Add(line);
            }

            sectors.Add(mapSector);
        }

        DetermineMirrors(sectors);
        RecalculateOffsets(sectors);
        DetermineSkyboxWalls(sectors);
        GenerateSpecialSkyboxTextures(sectors);

        return new Map
        {
            Player = new PlayerStart
            {
                ViewAngle = DetermineAngleInRadians(startingPosition.Angle),
                Where = (DetermineXLocation(startingPosition.PosX), DetermineYLocation(startingPosition.PosY), DetermineZLocation(startingPosition.PosZ)),
                Sector = startingPosition.SectorNumber
            },
            Sprites = ExtractSprites(sprites, grpSectors, spriteToAngleFrames),
            Sectors = sectors
        };

    }

    internal static Vector2 GetPoint(in WallType wall)
    {
        float x = DetermineXLocation(wall.X);
        float y = DetermineYLocation(wall.Y);

        return new Vector2(x, y);
    }

    internal static MapSector ParseSectorType(int index, in SectorType sector)
    {
        int ceiling = DetermineZLocation(sector.CeilingZ);
        int floor = DetermineZLocation(sector.FloorZ);

        string floorTexture = ToTile(sector.FloorPicNum);
        string ceilingTexture = ToTile(sector.CeilingPicNum);

        (int cXoffset, int cYOffset) = CalculateCeilingOffset(in sector, ceilingTexture);
        (int fXoffset, int fYOffset) = CalculateFloorOffset(in sector, floorTexture);

        (TextureRenderingOptions floorRenderingOptions, int floorXScale, int floorYScale) = ToTextureRenderingOptions(sector.FloorStat);
        (TextureRenderingOptions ceilingRenderingOptions, int ceilXScale, int ceilYScale) = ToTextureRenderingOptions(sector.CeilingStat);

        MapSectorSettings settings = default;

        if (ceilingRenderingOptions.HasFlag(TextureRenderingOptions.Sloped) && sector.CeilingHeiNum != 0f)
        {
            settings |= MapSectorSettings.SlopeCeiling;
        }

        if (ceilingRenderingOptions.HasFlag(TextureRenderingOptions.AlignWithFirstWall))
        {
            settings |= MapSectorSettings.RotateCeiling;
        }

        if (floorRenderingOptions.HasFlag(TextureRenderingOptions.Sloped) && sector.FloorHeiNum != 0f)
        {
            settings |= MapSectorSettings.SlopeFloor;
        }

        if (floorRenderingOptions.HasFlag(TextureRenderingOptions.AlignWithFirstWall))
        {
            settings |= MapSectorSettings.RotateFloor;
        }

        return new MapSector()
        {
            Id = index,
            Settings = settings,
            Ceiling = ceiling,
            Floor = floor,
            FloorTexture = new GameTextureInfo
            {
                Texture = TextureCache.GetTexture(floorTexture),
                XOffset = fXoffset,
                YOffset = fYOffset,
                XScale = floorXScale,
                YScale = floorYScale,
                RenderingOptions = floorRenderingOptions,
                Alpha = 1f,
                Palette = sector.FloorPal
            },
            CeilingTexture = new GameTextureInfo
            {
                Texture = TextureCache.GetTexture(ceilingTexture),
                XOffset = cXoffset,
                YOffset = cYOffset,
                XScale = ceilXScale,
                YScale = ceilYScale,
                RenderingOptions = ceilingRenderingOptions,
                Alpha = 1f,
                Palette = sector.CeilingPal
            },
            FloorShade = sector.FloorShade,
            CeilingShade = sector.CeilingShade,
            CeilingSlope = sector.CeilingHeiNum == 0f ? null : (sector.CeilingHeiNum / 4096f),
            FloorSlope = sector.FloorHeiNum == 0f ? null : (sector.FloorHeiNum / 4096f)
        };

    }

    internal static GameTextureInfo? GetTextureInfo(in WallType wall, in WallType textureWall, bool middleTexture)
    {
        short picNum;

        if (middleTexture && wall.NextSector != -1)
        {
            if (!wall.CStat.HasFlag(WallCStat.MaskingWall) && !wall.CStat.HasFlag(WallCStat.OneWayWall))
            {
                return null;
            }

            picNum = textureWall.OverPicNum;
        }
        else
        {
            picNum = textureWall.PicNum;
        }

        string textureName = ToTile(picNum);

        TextureRenderingOptions renderingOptions = ToTextureRenderingOptions(textureWall.CStat, !Unsafe.AreSame(in wall, in textureWall));

        (int xOffset, int yOffset) = CalculateOffset(in textureWall, textureName);
        float alpha = textureWall.CStat.HasFlag(WallCStat.Transluscence) ? 0.5f : 1.0f;

        // Lower Texture Specific
        if (!Unsafe.AreSame(in wall, in textureWall))
        {
            if ((textureWall.CStat ^ wall.CStat).HasFlag(WallCStat.XFlipped))
            {
                renderingOptions ^= TextureRenderingOptions.FlipX | TextureRenderingOptions.MirrorX;
            }
        }

        int scaleX = wall.XRepeat;
        int scaleY = wall.YRepeat;

        return new GameTextureInfo
        {
            Texture = TextureCache.GetTexture(textureName),
            XOffset = xOffset,
            YOffset = yOffset,
            XScale = scaleX,
            YScale = scaleY,
            RenderingOptions = renderingOptions,
            Alpha = alpha,
            Palette = wall.Pal
        };
    }

    private static void DetermineSkyboxWalls(List<MapSector> sectorList)
    {
        Span<MapSector> sectors = CollectionsMarshal.AsSpan(sectorList);

        foreach (MapSector sector in sectors)
        {
            bool ceilSkybox = sector.CeilingTexture.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox);
            bool floorSkybox = sector.FloorTexture.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox);

            if (ceilSkybox || floorSkybox)
            {
                foreach (Line line in sector.Walls)
                {
                    if (line.SectorTo is { } sectorTo && sectorTo != -1)
                    {
                        MapSector childSector = sectors[sectorTo];

                        bool ceilSkyboxChild = childSector.CeilingTexture.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox);
                        bool floorSkyboxChild = childSector.FloorTexture.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox);

                        if (ceilSkybox && ceilSkyboxChild)
                        {
                            line.UpperTexture.Texture = TextureCache.GetTexture(sector.CeilingTexture);
                            line.UpperTexture!.RenderingOptions |= TextureRenderingOptions.Skybox;
                        }

                        if (floorSkybox && floorSkyboxChild)
                        {
                            line.LowerTexture.Texture = TextureCache.GetTexture(sector.FloorTexture);
                            line.LowerTexture!.RenderingOptions |= TextureRenderingOptions.Skybox;
                        }
                    }
                }
            }
        }
    }

    private static void DetermineMirrors(List<MapSector> sectorList)
    {
        string placeHolderMirrorTexture = ToTile(560);
        string mirrorTexture = ToTile(503);

        Span<MapSector> sectors = CollectionsMarshal.AsSpan(sectorList);

        foreach (MapSector sector in sectors)
        {
            foreach (Line line in sector.Walls)
            {
                var middleTexture = line.MiddleTexture;

                if (middleTexture is null || line.SectorTo is null)
                {
                    continue;
                }

                if (middleTexture.Name != placeHolderMirrorTexture)
                {
                    continue;
                }

                line.IsMirror = true;
                line.SectorTo = sector.Id;

                middleTexture.Texture = TextureCache.GetTexture(mirrorTexture);
                middleTexture.RenderingOptions |= TextureRenderingOptions.Translucent;
                middleTexture.Alpha = 0.5f;
            }
        }
    }

    private static void RecalculateOffsets(List<MapSector> sectorList)
    {
        Span<MapSector> sectors = CollectionsMarshal.AsSpan(sectorList);

        for (int s = 0; s < sectors.Length; s++)
        {
            MapSector sector = sectors[s];
            Line firstWall = sector.Walls[0];

            if (sector.Settings.HasFlag(MapSectorSettings.SlopeFloor) || sector.Settings.HasFlag(MapSectorSettings.RotateFloor))
            {
                sector.RotationFloor = CalculateAngle(firstWall);
            }

            if (sector.Settings.HasFlag(MapSectorSettings.SlopeCeiling) || sector.Settings.HasFlag(MapSectorSettings.RotateCeiling))
            {
                sector.RotationCeiling = CalculateAngle(firstWall);
            }

            FixOffsets(sector.CeilingTexture);
            FixOffsets(sector.FloorTexture);

            foreach (Line line in sector.Walls)
            {
                if (line.SectorTo is { } sectorTo && sectorTo != -1)
                {
                    int sectorHeight = sector.Ceiling - sector.Floor;

                    MapSector neighborSector = sectors[sectorTo];
                    int floorOffset = neighborSector.Floor - sector.Floor;
                    int ceilOffset = neighborSector.Ceiling - sector.Ceiling;

                    if (floorOffset < 0)
                    {
                        floorOffset = 0;
                    }

                    if (ceilOffset > 0)
                    {
                        ceilOffset = 0;
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

                    GameTextureInfo lowerTextureInfo = line.LowerTexture!;
                    GameTextureInfo upperTextureInfo = line.UpperTexture!;

                    GameTexture lowerTexture = TextureCache.GetTexture(lowerTextureInfo);
                    GameTexture upperTexture = TextureCache.GetTexture(upperTextureInfo);

                    (float upperXScale, float upperYScale) = DetermineScale(upperTextureInfo, upperTexture);
                    (float lowerXScale, float lowerYScale) = DetermineScale(lowerTextureInfo, lowerTexture);

                    upperTextureInfo.YScale = upperYScale;
                    upperTextureInfo.XScale = upperXScale;
                    lowerTextureInfo.YScale = lowerYScale; // 0.0078125
                    lowerTextureInfo.XScale = lowerXScale; // 1

                    float windowEndY = sectorHeight - floorOffset;

                    if (lowerTextureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.FromSectorBottom))
                    {
                        float remainder = lowerYScale * windowEndY;
                        remainder -= MathF.Floor(remainder);

                        float potentialYOffset = upperTexture.Height - upperTexture.Height * remainder;

                        lowerTextureInfo.YOffset -= (int)potentialYOffset;
                        lowerTextureInfo.YOffset = SharedHelpers.EnsureOffsetIsPositive(lowerTextureInfo.Height, lowerTextureInfo.YOffset);
                        Debug.Assert(lowerTextureInfo.YOffset >= 0);
                    }

                    if (!upperTextureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.FromSectorBottom))
                    {
                        float remainder = upperYScale * ceilOffset;
                        remainder -= MathF.Floor(remainder);

                        float potentialYOffset = upperTexture.Height - upperTexture.Height * remainder;

                        upperTextureInfo.YOffset -= (int)potentialYOffset;
                        upperTextureInfo.YOffset = SharedHelpers.EnsureOffsetIsPositive(upperTexture.Height, upperTextureInfo.YOffset);
                        Debug.Assert(upperTextureInfo.YOffset >= 0);
                    }

                    if (line.MiddleTexture is { } middleTextureInfo)
                    {
                        (float middleXScale, float middleYScale) = DetermineScale(middleTextureInfo);

                        middleTextureInfo.YScale = middleYScale;
                        middleTextureInfo.XScale = middleXScale;
                    }
                }
                else
                {
                    GameTextureInfo middleTexture = line.MiddleTexture!;
                    int sectorHeight = sector.Ceiling - sector.Floor;

                    if (sectorHeight == 0)
                    {
                        continue;
                    }

                    (float xScale, float yScale) = DetermineScale(middleTexture);
                    middleTexture.YScale = yScale;
                    middleTexture.XScale = xScale;

                    float amountOnSector = yScale * sectorHeight;

                    // if 1:1 scaling with sector height, do nothing
                    if (amountOnSector != 1f)
                    {
                        if (middleTexture.RenderingOptions.HasFlag(TextureRenderingOptions.FromSectorBottom))
                        {
                            float remainder = amountOnSector - MathF.Floor(amountOnSector);

                            if (remainder != 0f)
                            {
                                float potentialYOffset = middleTexture.Height - middleTexture.Height * remainder;
                                middleTexture.YOffset += float.ConvertToIntegerNative<int>(potentialYOffset);
                                middleTexture.YOffset = SharedHelpers.EnsureOffsetIsPositive(middleTexture.Height, middleTexture.YOffset);

                                Debug.Assert(middleTexture.YOffset >= 0);
                            }
                            else if (middleTexture.YOffset != 0)
                            {
                                middleTexture.YOffset = middleTexture.Height - middleTexture.YOffset;
                                middleTexture.YOffset = SharedHelpers.EnsureOffsetIsPositive(middleTexture.Height, middleTexture.YOffset);

                                Debug.Assert(middleTexture.YOffset >= 0);
                            }
                        }
                    }
                }
            }
        }

        return;

        static void FixOffsets(GameTextureInfo textureInfo)
        {
            (int width, int height) = (textureInfo.Width, textureInfo.Height);

            if (textureInfo.RenderingOptions.IsFlippedX)
            {
                if (textureInfo.RenderingOptions.IsSwappedXY)
                {
                    textureInfo.YOffset = height - textureInfo.YOffset;
                }

                if (textureInfo.RenderingOptions.IsFlippedY)
                {
                    textureInfo.YOffset = height - textureInfo.YOffset;
                }
            }

            textureInfo.XOffset = SharedHelpers.EnsureOffsetIsPositive(width, textureInfo.XOffset);
            textureInfo.YOffset = SharedHelpers.EnsureOffsetIsPositive(height, textureInfo.YOffset);
        }

        static float CalculateAngle(Line firstWall)
        {
            (float x1, float y1) = firstWall.PointA.Point;
            (float x2, float y2) = firstWall.PointB.Point;

            float dy = y2 - y1;
            float dx = x2 - x1;

            float angle = MathF.Atan(dx / dy);

            if ((x2 - x1) < 0 || (y2 - y1) < 0)
                angle += MathF.PI;
            if ((x2 - x1) > 0 && (y2 - y1) < 0)
                angle -= MathF.PI;
            if (angle < 0)
                angle += MathF.PI * 2f;

            return angle - MathF.PI * 0.5f;
        }
    }

    private static void GenerateSpecialSkyboxTextures(List<MapSector> sectorList)
    {
        // Parallaxing Issues
        // https://infosuite.duke4.net/index.php?page=references_faq

        var moonSky1 = (BuildTexture)TextureCache.GetTexture(ToTile(80));
        var moonSky2 = (BuildTexture)TextureCache.GetTexture(ToTile(81));
        var moonSky3 = (BuildTexture)TextureCache.GetTexture(ToTile(82));
        var moonSky4 = (BuildTexture)TextureCache.GetTexture(ToTile(83));

        var bigOrbit1 = (BuildTexture)TextureCache.GetTexture(ToTile(84));
        var bigOrbit2 = (BuildTexture)TextureCache.GetTexture(ToTile(85));
        var bigOrbit3 = (BuildTexture)TextureCache.GetTexture(ToTile(86));
        var bigOrbit4 = (BuildTexture)TextureCache.GetTexture(ToTile(87));
        var bigOrbit5 = (BuildTexture)TextureCache.GetTexture(ToTile(88));

        var la1 = (BuildTexture)TextureCache.GetTexture(ToTile(89));
        var la2 = (BuildTexture)TextureCache.GetTexture(ToTile(90));
        var la3 = (BuildTexture)TextureCache.GetTexture(ToTile(91));
        var la4 = (BuildTexture)TextureCache.GetTexture(ToTile(92));
        var la5 = (BuildTexture)TextureCache.GetTexture(ToTile(93));

        BuildTexture moonSky = Combine("MOONSKY", moonSky1, moonSky2, moonSky3, moonSky4);
        BuildTexture bigOrbit = Combine("BIGORBIT", bigOrbit1, bigOrbit2, bigOrbit3, bigOrbit4, bigOrbit5);
        BuildTexture la = Combine("LA", la1, la2, la3, la4, la5);

        TextureCache.Add(moonSky.Name, moonSky);
        TextureCache.Add(bigOrbit.Name, bigOrbit);
        TextureCache.Add(la.Name, la);

        foreach (var sector in sectorList)
        {
            bool ceilSkybox = sector.CeilingTexture.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox);
            bool floorSkybox = sector.FloorTexture.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox);

            if (ceilSkybox)
            {
                ReplaceTexture(sector.CeilingTexture);
            }

            if (floorSkybox)
            {
                ReplaceTexture(sector.FloorTexture);
            }

            foreach (Line line in sector.Walls)
            {
                bool upperSkybox = line.UpperTexture?.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox) ?? false;
                bool lowerSkybox = line.LowerTexture?.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox) ?? false;

                if (upperSkybox)
                {
                    ReplaceTexture(line.UpperTexture!);
                }

                if (lowerSkybox)
                {
                    ReplaceTexture(line.LowerTexture!);
                }
            }
        }

        return;

        void ReplaceTexture(GameTextureInfo gameTextureInfo)
        {
            switch (gameTextureInfo.Name)
            {
                case "TILE_80":
                    gameTextureInfo.Texture = moonSky;
                    break;
                case "TILE_84":
                    gameTextureInfo.Texture = bigOrbit;
                    break;
                case "TILE_89":
                    gameTextureInfo.Texture = la;
                    break;
            }
        }

        static BuildTexture Combine(string textureName, params ReadOnlySpan<BuildTexture> textures)
        {
            int width = 0;
            int lookupSize = 0;
            for (int i = 0; i < textures.Length; i++)
            {
                BuildTexture texture = textures[i];
                width += texture.Width;
                lookupSize += texture.Lookup.Length;
            }

            byte[] combinedLookup = new byte[lookupSize];

            int xOffset = 0;
            for (int i = 0; i < textures.Length; i++)
            {
                BuildTexture texture = textures[i];

                for (int x = 0; x < texture.Width; x++)
                {
                    for (int y = 0; y < texture.Height; y++)
                    {
                        int textureIndex = y * texture.Width + x;
                        int combineTextureIndex = y * width + x + xOffset;

                        combinedLookup[combineTextureIndex] = texture.Lookup[textureIndex];
                    }
                }

                xOffset += texture.Width;
            }

            return new BuildTexture(textureName, width, textures[0].Height, combinedLookup);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (float XScale, float YScale) DetermineScale(GameTextureInfo textureInfo)
    {
        int xScale = (int)textureInfo.XScale!;
        int yScale = (int)textureInfo.YScale!;

        float x = ((float)(xScale << 3) / textureInfo.Width);
        float y = ((yScale / 16f) / textureInfo.Height);

        return (x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (float XScale, float YScale) DetermineScale(GameTextureInfo textureInfo, GameTexture wallTexture)
    {
        int xScale = (int)textureInfo.XScale!;
        int yScale = (int)textureInfo.YScale!;

        float x = ((float)(xScale << 3) / wallTexture.Width);
        float y = ((yScale / 16f) / wallTexture.Height);

        return (x, y);
    }

    private static (int XOffset, int YOffset) CalculateFloorOffset(in SectorType sector, string textureName)
    {
        // XPanning
        // Values are normalized on a 0-255 scale, meaning that regardless of the sprite's size, a value of 128 will pan it 50%.
        // https://wiki.eduke32.com/wiki/Xpanning

        GameTexture texture = TextureCache.GetTexture(textureName);

        int xOffset = 0;
        int yOffset = 0;
        byte xPanning = sector.FloorXPanning;
        byte yPanning = sector.FloorYPanning;

        if (xPanning != default)
        {
            int width = texture.Width;
            xOffset = (width << 16) / 256;
            xOffset = (xOffset * xPanning) >> 16;
        }

        if (yPanning != default)
        {
            int height = texture.Height;
            yOffset = (height << 16) / 256;
            yOffset = (yOffset * yPanning) >> 16;
        }

        return (xOffset, yOffset);
    }

    private static (int XOffset, int YOffset) CalculateCeilingOffset(in SectorType sector, string textureName)
    {
        // XPanning
        // Values are normalized on a 0-255 scale, meaning that regardless of the sprite's size, a value of 128 will pan it 50%.
        // https://wiki.eduke32.com/wiki/Xpanning

        GameTexture texture = TextureCache.GetTexture(textureName);

        int xOffset = 0;
        int yOffset = 0;
        byte xPanning = sector.CeilingXPanning;
        byte yPanning = sector.CeilingYPanning;

        if (xPanning != default)
        {
            int width = texture.Width;
            xOffset = (width << 16) / 256;
            xOffset = (xOffset * xPanning) >> 16;
        }

        if (yPanning != default)
        {
            int height = texture.Height;
            yOffset = (height << 16) / 256;
            yOffset = (yOffset * yPanning) >> 16;
        }

        return (xOffset, yOffset);
    }

    private static (int XOffset, int YOffset) CalculateOffset(in WallType wall, string textureName)
    {
        GameTexture texture = TextureCache.GetTexture(textureName);

        if (texture.Height > 128)
        {
            return (wall.XPanning, wall.YPanning);
        }

        if (texture.Height > 64)
        {
            return (wall.XPanning, wall.YPanning >> 1);
        }

        return (wall.XPanning, wall.YPanning >> 2);
    }

    private static Sprite[] ExtractSprites(Span<SpriteType> spritesTypes, Span<SectorType> grpSectors,
        Dictionary<int, SpriteAngleRotation[]> spriteToAngleFrames)
    {
        var lookup = ParseGameSpriteAnimation();
        Sprite[] sprites = new Sprite[spritesTypes.Length];

        for (int i = 0; i < spritesTypes.Length; i++)
        {
            ref SpriteType sprite = ref spritesTypes[i];

            ref SectorType sector = ref grpSectors[sprite.SectorNumber];

            float angle = DetermineAngleInRadians(sprite.Angle);

            string textureName = ToTile(sprite.PicNum);

            GameTexture texture = TextureCache.GetTexture(textureName);

            // On sprite Z location
            // "This is the actor's current z coordinate in the map. Note that unless the sprite's cstat has bit 8 (128) set, this position refers to the base of the sprite, not the center."
            // https://wiki.eduke32.com/wiki/Z
            int yRepeat = sprite.CStat.HasFlag(SpriteCStat.RealCentered) ? sprite.YRepeat >> 1 : sprite.YRepeat;
            int xRepeat = sprite.XRepeat;

            float elevation = DetermineZLocation(sprite.Z - sector.FloorZ);
            float textureHeight = (texture.Height * yRepeat) >> 5;

            float xScale = ((texture.Width * xRepeat) >> 5) / (float)texture.Width;
            float yScale = textureHeight / texture.Height;

            Dictionary<string, int>? additionalInfo = null;

            if (sprite.HiTag != default)
            {
                additionalInfo ??= [];
                additionalInfo.Add(nameof(SpriteType.HiTag), sprite.HiTag);
            }

            if (sprite.LoTag != default)
            {
                additionalInfo ??= [];
                additionalInfo.Add(nameof(SpriteType.LoTag), sprite.LoTag);
            }

            if (sprite.CStat.HasFlag(SpriteCStat.Wall))
            {
                sprites[i] = new WallSprite
                {
                    Id = i,
                    Angle = angle,
                    Location = new Vector2(DetermineXLocation(sprite.X), DetermineYLocation(sprite.Y)),
                    Height = elevation,
                    TwoSided = !sprite.CStat.HasFlag(SpriteCStat.OneSided),
                    Texture = new GameTextureInfo
                    {
                        Texture = TextureCache.GetTexture(textureName),
                        RenderingOptions = ToRenderingOptions(sprite.CStat),
                        XScale = xScale,
                        YScale = yScale,
                        Alpha = 1f,
                        XOffset = 0,
                        YOffset = 0,
                        Palette = sprite.Pal,
                    },
                    SectorId = sprite.SectorNumber,
                    Shade = sprite.Shade
                };
            }
            else if (sprite.CStat.HasFlag(SpriteCStat.Floor))
            {
                sprites[i] = new FloorSprite
                {
                    Id = i,
                    Angle = angle,
                    Location = new Vector2(DetermineXLocation(sprite.X), DetermineYLocation(sprite.Y)),
                    Height = elevation,
                    Texture = new GameTextureInfo
                    {
                        Texture = TextureCache.GetTexture(textureName),
                        RenderingOptions = ToRenderingOptions(sprite.CStat),
                        XScale = xScale,
                        YScale = yScale,
                        Alpha = 1f,
                        XOffset = 0,
                        YOffset = 0,
                        Palette = sprite.Pal,
                    },
                    SectorId = sprite.SectorNumber,
                    Shade = sprite.Shade
                };
            }
            else
            {
                _ = lookup.TryGetValue(sprite.PicNum, out GameSpriteAnimation? animationAngle);

                sprites[i] = new Sprite
                {
                    Id = i,
                    Angle = angle,
                    Location = new Vector2(DetermineXLocation(sprite.X), DetermineYLocation(sprite.Y)),
                    Height = elevation,
                    Texture = new GameTextureInfo
                    {
                        Texture = TextureCache.GetTexture(textureName),
                        RenderingOptions = ToRenderingOptions(sprite.CStat),
                        XScale = xScale,
                        YScale = yScale,
                        Alpha = 1f,
                        XOffset = 0,
                        YOffset = 0,
                        Palette = sprite.Pal
                    },
                    SectorId = sprite.SectorNumber,
                    Shade = sprite.Shade,
                    AnimationAngle = animationAngle
                };
            }
        }

        Precalculations.PrecalculateWallSprites(sprites);

        return sprites;

        static TextureRenderingOptions ToRenderingOptions(SpriteCStat stat)
        {
            TextureRenderingOptions options = default;

            if (stat.HasFlag(SpriteCStat.XFlipped))
            {
                options |= TextureRenderingOptions.FlipX;
            }

            if (stat.HasFlag(SpriteCStat.YFlipped))
            {
                options |= TextureRenderingOptions.FlipY;
            }

            if (stat.HasFlag(SpriteCStat.Translucent))
            {
                options |= TextureRenderingOptions.Translucent;
            }

            return options;
        }

        Dictionary<int, GameSpriteAnimation> ParseGameSpriteAnimation()
        {
            Dictionary<int, GameSpriteAnimation> lookup = [];

            foreach ((int key, var values) in spriteToAngleFrames)
            {
                var textureAngles = values.Select(static x => new TextureAngle(x.Angle ?? 0f, TextureCache.GetTexture(ToTile((short)x.Sprite)), x.Flipped))
                    .ToArray();

                lookup[key] = new GameSpriteAnimation
                {
                    AnimationToAngleToTexture = [textureAngles]
                };
            }

            return lookup;
        }
    }

    private static Dictionary<string, TextureInfo> ExtractTextures(List<ArtFile> artFiles)
    {
        Dictionary<string, TextureInfo> textures = [];

        Span<ArtFile> artFileSpan = CollectionsMarshal.AsSpan(artFiles);

        // Art File can have many tiles (textures)
        // Each tile is simply an X by Y index into the palette
        // where 255 is transparent

        for (int s = 0; s < artFileSpan.Length; s++)
        {
            ArtFile artFile = artFileSpan[s];

            short localTileNum = (short)artFile.LocalTileStart;

            for (int j = 0; j < artFile.Tiles.Length; j++)
            {
                TileType tile = artFile.Tiles[j];

                ReadOnlySpan<byte> pixels = tile.Pixels;

                if (pixels.Length != 0)
                {
                    byte[] texture = new byte[pixels.Length];

                    int i = 0;

                    for (int y = 0; y < tile.YSize; y++)
                    {
                        int index = y;

                        for (int x = 0; x < tile.XSize; x++)
                        {
                            byte palIndex = pixels[index];
                            texture[i] = palIndex;
                            i++;
                            index += tile.YSize;
                        }
                    }

                    string name = ToTile(localTileNum);

                    textures.Add(name, new TextureInfo(tile.XSize, tile.YSize, texture)
                    {
                        LeftOffset = tile.Properties.OffsetX,
                        TopOffset = tile.Properties.OffsetY,
                    });
                }

                localTileNum++;

            }
        }

        return textures;
    }

    internal sealed class TextureInfo
    {
        public int Width { get; }
        public int Height { get; }
        public byte[] Data { get; }
        public short LeftOffset { get; init; }
        public short TopOffset { get; init; }

        public TextureInfo(int width, int height, byte[] data)
        {
            Width = width;
            Height = height;
            Data = data;
        }
    }

    [SkipLocalsInit]
    private static ReadOnlySpan<BGRA> ToBGRA(ReadOnlySpan<RGB> rgb)
    {
        ref RGB color = ref MemoryMarshal.GetReference(rgb);

        Span<BGRA> bgra = new BGRA[rgb.Length];

        for (int i = 0; i < rgb.Length; i++)
        {
            // Calling "new BGRA" is extremely slow
            const uint Alpha = (uint)byte.MaxValue << 24;
            uint b = color.B;
            uint g = (uint)color.G << 8;
            uint r = (uint)color.R << 16;

            // Only 6-bits are used for color information, so each byte will need to be
            // multiplied by 4

            b <<= 2;
            g <<= 2;
            r <<= 2;

            bgra[i] = b | g | r | Alpha;

            color = ref Unsafe.Add(ref color, 1);
        }

        return bgra;
    }

    private static string ToTile(int tileNumber) => $"TILE_{tileNumber}";

    private static float DetermineYLocation(float coordinate)
    {
        coordinate /= 8f;
        return coordinate;
    }

    private static float DetermineXLocation(float coordinate)
    {
        coordinate /= 8f;
        return coordinate * -1;
    }

    private static int DetermineZLocation(int coordinate)
    {
        coordinate /= 128;
        // build engine coordinates are upside down
        return coordinate * -1;
    }

    private static float DetermineAngleInRadians(ushort angle)
    {
        return MathF.PI * (angle / 1024f);
    }
}
