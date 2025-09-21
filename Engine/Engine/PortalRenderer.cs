using RenderingEngine.Models;
using RenderingEngine.TextureManagement;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        public readonly int PixelWidth;
        public readonly int PixelHeight;
        public readonly float VFov;
        public required Player Player { get; set; }
        public required Sector[] Sectors { get; set; }

        private readonly WallHelper WallHelper;
        private readonly RenderWindowHelper RenderWindowHelper;

        private PortalPlayerSnapshot? Snapshot = null;

        private readonly float[] distanceCache;
        private readonly uint[] distanceMult;
        private readonly BGRA[] buffer;

        public PortalRenderer(int width, int height)
        {
            PixelWidth = width;
            PixelHeight = height;
            VFov = .3f * height;
            WallHelper = new WallHelper(width, height, EngineConstants.CameraPlaneX, VFov);
            buffer = new BGRA[width * height];
            distanceCache = new float[height];
            distanceMult = new uint[height];

            RenderWindowHelper = new RenderWindowHelper(width, height);
        }

        private void GenerateDistanceCache(PortalPlayerSnapshot player, Sector sector)
        {
            var distanceArray = distanceCache;

            int height = PixelHeight;
            float vFov = VFov;
            int halfHeightInt = height / 2;
            float oneOvervFov = 1f / vFov;
            var yaw = player.Yaw;

            (_, _, float pz) = player.Where;

            float yfloor = sector.Floor - pz;
            float yCeil = sector.Ceil - pz;

            for (int i = halfHeightInt + 1; i < height; i++)
            {
                int j = halfHeightInt - i;

                float yMopPosR = yfloor / (j * oneOvervFov + yaw);
                float distance = yMopPosR + 1;
                distanceArray[i] = distance;

                float brightness = 1f - EngineConstants.OneOverLightFallOffDistance * distance;
                distanceMult[i] = (uint)(brightness * 255f);
            }

            for (int i = 0; i < halfHeightInt; i++)
            {
                int j = halfHeightInt - i;

                float yMopPosR = yCeil / (j * oneOvervFov + yaw);
                float distance = yMopPosR + 1;
                distanceArray[i] = distance;

                float brightness = 1f - EngineConstants.OneOverLightFallOffDistance * distance;
                distanceMult[i] = (uint)(brightness * 255f);
            }
        }

        public void DrawScreen(Span<BGRA> screen, PortalPlayerSnapshot player)
        {
            TextureInfo wallTexture = TextureLoader.GetTexture(TextureName.Rock, true);
            float yaw = player.Yaw;

            (float pSin, float pCos) = MathF.SinCos(player.Angle);
            (float px, float py, float pz) = player.Where;
            screen.Fill(BGRA.Green);

            ReadOnlySpan<Sector> sectors = Sectors;

            RenderWindowHelper.NewRender();
            Span<(int top, int bottom)> portalTopBottom = RenderWindowHelper.Portal;

            Queue<NeighborsToRender> sectorRenderQueue = [];
            sectorRenderQueue.Enqueue(new NeighborsToRender
            {
                SectorId = player.Sector,
                FromX = 0,
                ToX = PixelWidth
            });

            List<Wall> wallsRendered = [];

            do
            {
                NeighborsToRender sectorInfo = sectorRenderQueue.Dequeue();

                Sector sector = sectors[sectorInfo.SectorId];

                float yceil = sector.Ceil - pz;
                float yfloor = sector.Floor - pz;

                Span<Wall> walls = WallHelper.DetermineWallsToRender(sector,
                    wallsRendered,
                    pSin, pCos, px, py, yceil, yfloor, yaw);

                Dictionary<int, NeighborsToRender>.ValueCollection neighbors = RenderSector(player, sector, sectors, sectorInfo, walls, screen, wallTexture, portalTopBottom);

                foreach (NeighborsToRender neighbor in neighbors)
                {
                    wallsRendered.AddRange(neighbor.Walls.Select(x => x.Wall));
                    sectorRenderQueue.Enqueue(neighbor);
                }
            }
            while (sectorRenderQueue.Count > 0);
        }

        private Dictionary<int, NeighborsToRender>.ValueCollection RenderSector(
            PortalPlayerSnapshot player,
            Sector sector,
            ReadOnlySpan<Sector> sectors,
            NeighborsToRender sectorInfo,
            Span<Wall> walls,
            Span<BGRA> screen,
            TextureInfo wallTexture,
            Span<(int top, int bottom)> portalTopBottom)
        {
            GenerateDistanceCache(player, sector);

            Dictionary<int, NeighborsToRender> neightbors = [];

            RenderWindowHelper.NewSector(sectorInfo);

            for (int s = 0; s < walls.Length; s++)
            {
                Wall wall = walls[s];

                (bool wallDrawn, int wallFromX, int wallToX) = wall.Neighbor == EngineConstants.NullSector ?
                    DrawBasicWall(screen, wallTexture, wall) :
                    DrawPortalWall(sector, sectors, screen, wallTexture, portalTopBottom, wall);

                if (wallDrawn && wall.Neighbor != EngineConstants.NullSector)
                {
                    if (!neightbors.TryGetValue(wall.Neighbor, out NeighborsToRender? neighborsToRender))
                    {
                        neighborsToRender = new NeighborsToRender { SectorId = wall.Neighbor };
                        neightbors.Add(wall.Neighbor, neighborsToRender);
                    }

                    neighborsToRender.Walls.Add(new WallToRender { Wall = wall, FromX = wallFromX, ToX = wallToX });
                }
            }

            TextureInfo groundTexture = TextureLoader.GetTexture(TextureName.CaveGround, false);
            TextureInfo ceilingTexture = TextureLoader.GetTexture(TextureName.CeilingOffice, false);

            if (Vector.IsHardwareAccelerated)
            {
                RenderFloorVector(player, sector, screen, groundTexture);
                RenderCeilingVector(player, sector, screen, ceilingTexture);
            }
            else
            {
                RenderFloor(player, sector, screen, groundTexture);
                RenderCeiling(player, sector, screen, ceilingTexture);
            }

            return neightbors.Values;
        }

        private void DebugStuffs(
            Span<BGRA> screen,
            Span<(int top, int bottom)> renderedArea)
        {
            int height = PixelWidth;

            for (int x = 0; x < PixelWidth; x++)
            {
                (int renderedFrom, int renderedTo) = renderedArea[x];

                if (renderedFrom != -1)
                {
                    screen[renderedFrom * height + x] = BGRA.Red;

                    if (renderedFrom != PixelHeight - 1)
                    {
                        screen[(renderedFrom + 1) * height + x] = BGRA.Red;
                    }
                }

                if (renderedTo != -1)
                {
                    screen[renderedTo * height + x] = BGRA.Blue;

                    if (renderedTo != 0)
                    {
                        screen[(renderedTo - 1) * height + x] = BGRA.Blue;
                    }
                }
            }
        }

        private void DebugStuffs2(
            Span<BGRA> screen,
            Span<(int top, int bottom)> renderedArea)
        {
            int height = PixelWidth;

            for (int x = 0; x < PixelWidth; x++)
            {
                (int renderedFrom, int renderedTo) = renderedArea[x];

                if (renderedFrom != -1)
                {
                    screen[renderedFrom * height + x] = BGRA.Black;


                    if (renderedFrom != PixelHeight - 1)
                    {
                        screen[(renderedFrom + 1) * height + x] = BGRA.Black;
                    }
                }

                if (renderedTo != -1)
                {
                    screen[renderedTo * height + x] = BGRA.White;

                    if (renderedTo != 0)
                    {
                        screen[(renderedTo - 1) * height + x] = BGRA.White;
                    }
                }
            }
        }

        private (bool WallDrawn, int WallFromX, int WallToX) DrawPortalWall(
            Sector sector,
            ReadOnlySpan<Sector> sectors,
            Span<BGRA> screen,
            TextureInfo wallTexture,
            Span<(int top, int bottom)> portalTopBottom,
            Wall wall)
        {
            if (!RenderWindowHelper.SetWallToRender(wall))
            {
                return (false, default, default);
            }

            bool wallDrawn = false;

            (int wallFromXOffset, int wallFromX, int wallToX) = RenderWindowHelper.GetWallRenderWindowX();
            WallYPlaneInfo yPlaneInfo = WallHelper.CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);

            // wall plane
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            int width = PixelWidth;
            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;

            // for player camera position / ray
            float rx1 = wall.X1;
            float ry1 = wall.Y1;
            float rx2 = wall.X2;
            float ry2 = wall.Y2;
            float d2x = rx2 - rx1;
            float d2y = ry2 - ry1;
            float t1 = rx1 * d2y - ry1 * d2x;
            float cameraRay = -1f * EngineConstants.CameraPlaneX;
            cameraRay += cameraWidthIncr * wallFromX;

            //
            Sector neighborSector = sectors[wall.Neighbor];
            float sectorHeight = 1f / (sector.Ceil - sector.Floor);
            float floorOffset = neighborSector.Floor - sector.Floor;
            float ceilOffset = neighborSector.Ceil - sector.Ceil;

            ref BGRA wallTexturePtr = ref wallTexture.Texture;
            ref uint screenPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(screen));

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                int wallStartYInt = (int)wallStartY;
                int wallEndYInt = (int)wallEndY;

                var result = RenderWindowHelper.TryGetRenderableDimensionsForX(x, wallStartYInt, wallEndYInt);

                if (!result.CanRender)
                {
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                int portalFromY = result.PortalFromY;
                int portalToY = result.PortalToY;
                int clamptedFromY = result.ClampedFromY;
                int clamptedToY = result.ClampedToY;

                float denominator = cameraRay * d2y - d2x;
                float fromToYDist = t1 / denominator;
                float fromToXDist = fromToYDist * cameraRay;

                float distX = rx1 - fromToXDist;
                float distY = ry1 - fromToYDist;
                float distance = MathF.Sqrt(distX * distX + distY * distY);

                float brightness = 1f - EngineConstants.OneOverLightFallOffDistance * fromToYDist;

                {
                    float pixelsPerHeight = (wallEndYInt - wallStartYInt) * sectorHeight;
                    int floorPixelOffset = (int)(pixelsPerHeight * floorOffset);
                    int ceilPixelOffset = (int)(pixelsPerHeight * ceilOffset);

                    // neighbor ?
                    int fromYN = wallStartYInt - ceilPixelOffset;
                    int toYN = wallEndYInt - floorPixelOffset;
                    fromYN = Math.Clamp(fromYN, portalFromY, portalToY);
                    toYN = Math.Clamp(toYN, portalFromY, portalToY);

                    portalFromY = Math.Max(fromYN, clamptedFromY);
                    portalToY = Math.Min(toYN, clamptedToY);

                    float textureXIncr = 64f / (wallEndYInt - wallStartYInt);
                    int textureYPos = (int)(distance * 64f) % 64;

                    ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, clamptedFromY * PixelWidth + x);
                    float textureXPos = textureXIncr * Math.Abs(wallStartYInt - clamptedFromY);
                    int textureYPosI = textureYPos << 6;

                    uint shaded = default;
                    int textureXPosIOld = -1;

                    for (int y = clamptedFromY; y <= portalFromY; ++y)
                    {
                        int textureXPosI = (int)textureXPos;

                        if (textureXPosI != textureXPosIOld)
                        {
                            textureXPosIOld = textureXPosI;
                            ref BGRA textureIndexPtr = ref Unsafe.Add(ref wallTexturePtr, textureYPosI + textureXPosI);

                            shaded = ShadeByBrightness2(textureIndexPtr, brightness);
                        }

                        screenIndexPtr = shaded;
                        screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, PixelWidth);
                        textureXPos += textureXIncr;
                    }

                    screenIndexPtr = ref Unsafe.Add(ref screenPtr, portalToY * PixelWidth + x);
                    textureXPos = textureXIncr * Math.Abs(wallStartYInt - portalToY);
                    shaded = default;
                    textureXPosIOld = -1;

                    for (int y = portalToY; y <= clamptedToY; ++y)
                    {
                        int textureXPosI = (int)textureXPos;

                        if (textureXPosI != textureXPosIOld)
                        {
                            textureXPosIOld = textureXPosI;
                            ref BGRA textureIndexPtr = ref Unsafe.Add(ref wallTexturePtr, textureYPosI + textureXPosI);
                            shaded = ShadeByBrightness2(textureIndexPtr, brightness);
                        }

                        screenIndexPtr = shaded;
                        screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, PixelWidth);
                        textureXPos += textureXIncr;
                    }
                }

                // portal is between wall and ceiling
                portalTopBottom[x] = (portalFromY, portalToY);

                wallStartY += ceilDistIncr;
                wallEndY += floorDistIncr;
                wallDrawn = true;
            }

            return (wallDrawn, wallFromX, wallToX);
        }

        private (bool WallDrawn, int WallFromX, int WallToX) DrawBasicWall(
            Span<BGRA> screen,
            TextureInfo wallTexture,
            Wall wall)
        {
            if (!RenderWindowHelper.SetWallToRender(wall))
            {
                return (false, default, default);
            }

            (int wallFromXOffset, int wallFromX, int wallToX) = RenderWindowHelper.GetWallRenderWindowX();
            WallYPlaneInfo yPlaneInfo = WallHelper.CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);

            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            int width = PixelWidth;
            float cameraWidthIncr = 2.0f / width * EngineConstants.CameraPlaneX;

            // for player camera position / ray
            float rx1 = wall.X1;
            float ry1 = wall.Y1;
            float rx2 = wall.X2;
            float ry2 = wall.Y2;
            float d2x = rx2 - rx1;
            float d2y = ry2 - ry1;
            float t1 = rx1 * d2y - ry1 * d2x;
            float cameraRay = -1f * EngineConstants.CameraPlaneX;
            cameraRay += cameraWidthIncr * wallFromX;

            ref BGRA wallTexturePtr = ref wallTexture.Texture;
            ref uint screenPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(screen));

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                int wallStartYInt = (int)wallStartY;
                int wallEndYInt = (int)wallEndY;

                var result = RenderWindowHelper.TryGetRenderableDimensionsForX(x, wallStartYInt, wallEndYInt);

                if (!result.CanRender)
                {
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                int portalFromY = result.PortalFromY;
                int portalToY = result.PortalToY;
                int clamptedFromY = result.ClampedFromY;
                int clamptedToY = result.ClampedToY;

                float denominator = cameraRay * d2y - d2x;
                float fromToYDist = t1 / denominator;
                float fromToXDist = fromToYDist * cameraRay;

                float distX = rx1 - fromToXDist;
                float distY = ry1 - fromToYDist;
                float distance = MathF.Sqrt(distX * distX + distY * distY);

                float brightness = 1f - EngineConstants.OneOverLightFallOffDistance * fromToYDist;

                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, clamptedFromY * PixelWidth + x);
                int textureYPos = (int)(distance * 64f) % 64;
                float textureXIncr = 64f / (wallEndYInt - wallStartYInt);
                float textureXPos = textureXIncr * Math.Abs(wallStartYInt - clamptedFromY);
                int textureYPosI = textureYPos << 6;

                uint shaded = default;
                int textureXPosIOld = -1;

                for (int y = clamptedFromY; y <= clamptedToY; ++y)
                {
                    int textureXPosI = (int)textureXPos;

                    if (textureXPosI != textureXPosIOld)
                    {
                        textureXPosIOld = textureXPosI;
                        ref BGRA textureIndexPtr = ref Unsafe.Add(ref wallTexturePtr, textureYPosI + textureXPosI);
                        shaded = ShadeByBrightness2(textureIndexPtr, brightness);
                    }

                    screenIndexPtr = shaded;
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, PixelWidth);
                    textureXPos += textureXIncr;
                }

                wallStartY += ceilDistIncr;
                wallEndY += floorDistIncr;
            }

            return (true, wallFromX, wallToX);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ShadeByPrecalc(ref BGRA inColor, ref BGRA outColor, ref uint scale)
        {
            unchecked
            {
                const uint Alpha = (uint)byte.MaxValue << 24;

                uint b = inColor.B * scale >> 8;
                uint g = inColor.G * scale >> 8 << 8;
                uint r = inColor.R * scale >> 8 << 16;

                uint bgra = b | g | r | Alpha;

                Unsafe.As<BGRA, uint>(ref outColor) = bgra;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ShadeByBrightness2(BGRA inColor, float brightness)
        {
            const uint Alpha = (uint)byte.MaxValue << 24;

            if (brightness <= 0)
            {
                return Alpha;
            }

            unchecked
            {
                uint scale = (uint)(brightness * 255f);

                uint b = inColor.B * scale >> 8;
                uint g = inColor.G * scale >> 8 << 8;
                uint r = inColor.R * scale >> 8 << 16;

                return b | g | r | Alpha;
            }
        }

        [MemberNotNull(nameof(Snapshot))]
        public BGRA[] DrawFrame(PortalPlayerSnapshot snapShot)
        {
            Snapshot = snapShot;

            Span<BGRA> currentBuffer = buffer;
            DrawScreen(currentBuffer, snapShot);

            return buffer;
        }
    }
}
