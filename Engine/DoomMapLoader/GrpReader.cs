using BuildAssetLoader;
using BuildAssetLoader.Map;
using BuildAssetLoader.Texture;
using RenderingEngine.Engine;
using RenderingEngine.Models;
using SkiaSharp;

namespace RenderingEngine.DoomMapLoader
{
    internal static class GrpReader
    {
        public static Map LoadBuildMap(GrpFile grp, string mapName)
        {
            var map = BuildFileParser.ExtractMapFiles(grp);

            return ExtractBuildMap(map.Single(x => x.MapName == mapName));
        }

        public static void ExtractAllTextures(GrpFile grp, PaletteFile paletteFile)
        {
            List<ArtFile> artFiles = BuildFileParser.ExtractArtFiles(grp);
            Dictionary<string, TextureInfo> textures = ExtractTextures(artFiles);

            ReadOnlySpan<BGRA> pal = ToBGRA(MemoryMarshal.Cast<byte, RGB>(paletteFile.Palette));

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

                TextureCache.AddPallette(i, palette);
            }

            foreach ((string name, var info) in textures)
            {
                TextureCache.Add(name, new BuildTexture(info.Width, info.Height, info.Data));
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

            return (options, xScale, yScale);
        }

        private static TextureRenderingOptions ToTextureRenderingOptions(WallCStat stat)
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
                options |= TextureRenderingOptions.FlipX;
            }

            if (stat.HasFlag(WallCStat.YFlipped))
            {
                options |= TextureRenderingOptions.FlipY;
            }

            if (stat.HasFlag(WallCStat.Rotate90))
            {
                throw new NotImplementedException();
            }

            return options;
        }

        private static Map ExtractBuildMap(MapFile mapFile)
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

                    var line = new Line {
                        Id = ij,
                        PointA = new LineVector(j, GetPoint(in wall)),
                        PointB = new LineVector(wall.Point2, GetPoint(in point2Wall)),
                        SectorTo = wall.NextSector,
                        UpperTexture = GetTextureInfo(in wall, in wall, false),
                        MiddleTexture = GetTextureInfo(in wall, in wall, true),
                        LowerTexture = GetTextureInfo(in wall, in nextWall, false),
                        Shade = wall.Shade
                    };

                    ij++;

                    mapSector.Walls.Add(line);
                }

                sectors.Add(mapSector);
            }

            RecalculateOffsets(sectors);
            DetermineSkyboxWalls(sectors);

            return new Map
            {
                Player = new Player
                {
                    Angle = DetermineAngleInRadians(startingPosition.Angle),
                    Where = (DetermineXLocation(startingPosition.PosX), DetermineYLocation(startingPosition.PosY), DetermineZLocation(startingPosition.PosZ)),
                    Sector = startingPosition.SectorNumber
                },
                Sprites = ExtractSprites(sprites, grpSectors),
                Sectors = sectors
            };

        }

        internal static Point GetPoint(in WallType wall)
        {
            float x = DetermineXLocation(wall.X);
            float y = DetermineYLocation(wall.Y);

            return new Point(x, y);
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

            if (sector.CeilingHeiNum != 0f)
            {
                settings |= MapSectorSettings.SlopeCeiling;
            }

            if (ceilingRenderingOptions.HasFlag(TextureRenderingOptions.AlignWithFirstWall))
            {
                settings |= MapSectorSettings.RotateCeiling;
            }

            if (sector.FloorHeiNum != 0f)
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
                FloorTexture = new Models.TextureInfo
                {
                    Name = floorTexture,
                    XOffset = fXoffset,
                    YOffset = fYOffset,
                    XScale = floorXScale,
                    YScale = floorYScale,
                    RenderingOptions = floorRenderingOptions,
                    Alpha = 1f
                },
                CeilingTexture = new Models.TextureInfo
                {
                    Name = ceilingTexture,
                    XOffset = cXoffset,
                    YOffset = cYOffset,
                    XScale = ceilXScale,
                    YScale = ceilYScale,
                    RenderingOptions = ceilingRenderingOptions,
                    Alpha = 1f
                },
                FloorShade = sector.FloorShade,
                CeilingShade = sector.CeilingShade,
                CeilingSlope = sector.CeilingHeiNum == 0f ? null : (sector.CeilingHeiNum / 4096f),
                FloorSlope = sector.FloorHeiNum == 0f ? null : (sector.FloorHeiNum / 4096f)
            };

        }

        private static void PrecalculateWallSprites(scoped ReadOnlySpan<Sprite> sprites)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                Models.TextureInfo texture = sprite.Texture;

                (float width, float height) = texture.GetScaledDemensions();

                (float x, float y) = sprite.Location;

                // calculate the x, y for the wall on the screen for both points
                float rx1 = x - width * 0.5f;
                float rx2 = x + width * 0.5f;
                float ry1 = y;
                float ry2 = y;

                if (sprite is WallSprite wallSprite)
                {
                    (float sin, float cos) = MathF.SinCos(wallSprite.Angle);

                    (rx1, ry1) = SharedHelpers.RotateVertex(rx1, ry1, sin, cos, x, y);
                    (rx2, ry2) = SharedHelpers.RotateVertex(rx2, ry2, sin, cos, x, y);

                    rx1 += x;
                    ry1 += y;
                    rx2 += x;
                    ry2 += y;
                }
                else if (sprite is FloorSprite floorSprite)
                {
                    (float sin, float cos) = MathF.SinCos(floorSprite.Angle);

                    float xFrom = x - width * 0.5f;
                    float xTo = x + width * 0.5f;
                    float yTo = y - height * 0.5f;
                    float yFrom = y + height * 0.5f;

                    floorSprite.PointA = SharedHelpers.RotateVertexAroundPoint(xFrom, yFrom, sin, cos, x, y);
                    floorSprite.PointB = SharedHelpers.RotateVertexAroundPoint(xTo, yFrom, sin, cos, x, y);
                    floorSprite.PointC = SharedHelpers.RotateVertexAroundPoint(xFrom, yTo, sin, cos, x, y);
                    floorSprite.PointD = SharedHelpers.RotateVertexAroundPoint(xTo, yTo, sin, cos, x, y);

                    continue;
                }

                sprite.Length = width;
                sprite.PointA = new Point(rx1, ry1);
                sprite.PointB = new Point(rx2, ry2);
            }
        }


        internal static Models.TextureInfo? GetTextureInfo(in WallType wall, in WallType textureWall, bool middleTexture)
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
            (int xOffset, int yOffset) = CalculateOffset(in wall, textureName);

            TextureRenderingOptions renderingOptions = ToTextureRenderingOptions(textureWall.CStat);

            float alpha = textureWall.CStat.HasFlag(WallCStat.Transluscence) ? 0.5f : 1.0f;

            int scaleX = wall.XRepeat;
            int scaleY = wall.YRepeat;

            return new Models.TextureInfo
            {
                Name = textureName,
                XOffset = xOffset,
                YOffset = yOffset,
                XScale = scaleX,
                YScale = scaleY,
                RenderingOptions = renderingOptions,
                Alpha = alpha
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
                        if (line.SectorTo is int sectorTo && sectorTo != -1)
                        {
                            var childSector = sectors[sectorTo];

                            bool ceilSkyboxChild = childSector.CeilingTexture.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox);
                            bool floorSkyboxChild = childSector.FloorTexture.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox);

                            if (ceilSkybox && ceilSkyboxChild)
                            {
                                line.UpperTexture.Name = sector.CeilingTexture.Name;
                                line.UpperTexture!.RenderingOptions |= TextureRenderingOptions.Skybox;
                            }

                            if (floorSkybox && floorSkyboxChild)
                            {
                                line.LowerTexture.Name = sector.FloorTexture.Name;
                                line.LowerTexture!.RenderingOptions |= TextureRenderingOptions.Skybox;
                            }
                        }
                    }
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
                    if (line.SectorTo is int sectorTo && sectorTo != -1)
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

                        Models.TextureInfo lowerTextureInfo = line.LowerTexture!;
                        Models.TextureInfo upperTextureInfo = line.UpperTexture!;

                        Texture lowerTexture = TextureCache.GetTexture(lowerTextureInfo);
                        Texture upperTexture = TextureCache.GetTexture(upperTextureInfo);

                        (float upperXScale, float upperYScale) = DetermineScale(upperTextureInfo, upperTexture);
                        (float lowerXScale, float lowerYScale) = DetermineScale(lowerTextureInfo, lowerTexture);

                        upperTextureInfo.YScale = upperYScale;
                        upperTextureInfo.XScale = upperXScale;
                        lowerTextureInfo.YScale = lowerYScale;
                        lowerTextureInfo.XScale = lowerXScale;

                        float windowEndY = sectorHeight - floorOffset;

                        if (lowerTextureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.FromSectorBottom))
                        {
                            float remainder = lowerYScale * windowEndY;
                            remainder = remainder - MathF.Floor(remainder);

                            float potentialYOffset = upperTexture.Height - upperTexture.Height * remainder;

                            lowerTextureInfo.YOffset -= (int)potentialYOffset;
                        }

                        if (!upperTextureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.FromSectorBottom))
                        {
                            float remainder = upperYScale * ceilOffset;
                            remainder = remainder - MathF.Floor(remainder);

                            float potentialYOffset = upperTexture.Height - upperTexture.Height * remainder;

                            upperTextureInfo.YOffset -= (int)potentialYOffset;
                        }

                        if (line.MiddleTexture is Models.TextureInfo middleTextureInfo)
                        {
                            (float middleXScale, float middleYScale) = DetermineScale(middleTextureInfo);

                            middleTextureInfo.YScale = middleYScale;
                            middleTextureInfo.XScale = middleXScale;
                        }
                    }
                    else
                    {
                        Models.TextureInfo middleTexture = line.MiddleTexture!;
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
                                }
                                else if (middleTexture.YOffset != 0)
                                {
                                    middleTexture.YOffset = middleTexture.Height - middleTexture.YOffset;
                                }
                            }
                        }
                    }
                }
            }

            static void FixOffsets(Models.TextureInfo textureInfo)
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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (float XScale, float YScale) DetermineScale(Models.TextureInfo textureInfo)
        {
            int xScale = (int)textureInfo.XScale!;
            int yScale = (int)textureInfo.YScale!;

            float x = ((float)(xScale << 3) / textureInfo.Width);
            float y = ((yScale / 16f) / textureInfo.Height);

            return (x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (float XScale, float YScale) DetermineScale(Models.TextureInfo textureInfo, Texture wallTexture)
        {
            int xScale = (int)textureInfo.XScale!;
            int yScale = (int)textureInfo.YScale!;

            float x = ((float)(xScale << 3) / wallTexture.Width);
            float y = ((yScale / 16f) / wallTexture.Height);

            return (x, y);
        }

        private static int DetermineTextureOffsetFromBottom(float textureHeight, float sectorHeight)
        {
            if (sectorHeight >= textureHeight)
            {
                return float.ConvertToIntegerNative<int>(sectorHeight % textureHeight);
            }
            else
            {
                return float.ConvertToIntegerNative<int>(textureHeight - sectorHeight);
            }
        }

        private static (int XOffset, int YOffset) CalculateFloorOffset(in SectorType sector, string textureName)
        {
            // XPanning
            // Values are normalized on a 0-255 scale, meaning that regardless of the sprite's size, a value of 128 will pan it 50%.
            // https://wiki.eduke32.com/wiki/Xpanning

            Texture texture = TextureCache.GetTexture(textureName);

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

            Texture texture = TextureCache.GetTexture(textureName);

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
            Texture texture = TextureCache.GetTexture(textureName);

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

        private static Sprite[] ExtractSprites(Span<SpriteType> spritesTypes, Span<SectorType> grpSectors)
        {
            Sprite[] sprites = new Sprite[spritesTypes.Length];

            for (int i = 0; i < spritesTypes.Length; i++)
            {
                ref SpriteType sprite = ref spritesTypes[i];

                ref SectorType sector = ref grpSectors[sprite.SectorNumber];

                float angle = DetermineAngleInRadians(sprite.Angle);

                string textureName = ToTile(sprite.PicNum);

                Texture texture = TextureCache.GetTexture(textureName);

                // On sprite Z location
                // "This is the actor's current z coordinate in the map. Note that unless the sprite's cstat has bit 8 (128) set, this position refers to the base of the sprite, not the center."
                // https://wiki.eduke32.com/wiki/Z
                int yRepeat = sprite.CStat.HasFlag(SpriteCStat.RealCentered) ? sprite.YRepeat >> 1 : sprite.YRepeat;
                int xRepeat = sprite.XRepeat;

                float elevation = DetermineZLocation(sprite.Z - sector.FloorZ);
                float textureHeight = (texture.Height * yRepeat) >> 5;

                float xScale = ((texture.Width * xRepeat) >> 5) / (float)texture.Width;
                float yScale = textureHeight / texture.Height;

                if (sprite.CStat.HasFlag(SpriteCStat.Wall))
                {
                    sprites[i] = new WallSprite
                    {
                        Id = i,
                        Angle = angle,
                        Location = new Point(DetermineXLocation(sprite.X), DetermineYLocation(sprite.Y)),
                        Height = elevation,
                        TwoSided = !sprite.CStat.HasFlag(SpriteCStat.OneSided),
                        Texture = new Models.TextureInfo
                        {
                            Name = textureName,
                            RenderingOptions = ToRenderingOptions(sprite.CStat),
                            XScale = xScale,
                            YScale = yScale
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
                        Location = new Point(DetermineXLocation(sprite.X), DetermineYLocation(sprite.Y)),
                        Height = elevation,
                        Texture = new Models.TextureInfo
                        {
                            Name = textureName,
                            RenderingOptions = ToRenderingOptions(sprite.CStat),
                            XScale = xScale,
                            YScale = yScale
                        },
                        SectorId = sprite.SectorNumber,
                        Shade = sprite.Shade
                    };
                }
                else
                {
                    sprites[i] = new Sprite
                    {
                        Id = i,
                        Angle = angle,
                        Location = new Point(DetermineXLocation(sprite.X), DetermineYLocation(sprite.Y)),
                        Height = elevation,
                        Texture = new Models.TextureInfo
                        {
                            Name = textureName,
                            RenderingOptions = ToRenderingOptions(sprite.CStat),
                            XScale = xScale,
                            YScale = yScale
                        },
                        SectorId = sprite.SectorNumber,
                        Shade = sprite.Shade
                    };
                }
            }

            PrecalculateWallSprites(sprites);

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

                return options;
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

        private static string ToTile(short tileNumber) => $"TILE_{tileNumber}";

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

        private static unsafe void DebugTexture(int width, int height, Span<BGRA> texture, string textureName)
        {
            textureName = textureName.Replace("\\", "_");

            var info = new SKImageInfo(width, height)
            {
                AlphaType = SKAlphaType.Premul,
                ColorType = SKColorType.Bgra8888,
            };

            fixed (BGRA* bgraPtr = &texture[0])
            {
                var image = SKImage.FromPixels(info, (nint)bgraPtr, info.RowBytes);

                using var data = image.Encode(SKEncodedImageFormat.Png, 100); // 100 = max quality
                string outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"C:\\Users\\Alexa\\Downloads\\New folder\\tex2\\{textureName}.PNG");
                using (var stream = File.OpenWrite(outputPath))
                {
                    data.SaveTo(stream);
                }

                Console.WriteLine($"Image saved to {outputPath}");
            }
        }
    }
}
