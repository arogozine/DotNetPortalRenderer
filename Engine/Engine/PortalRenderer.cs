using RenderingEngine.Models;
using RenderingEngine.TextureManagement;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
            screen.Fill(BGRA.Black);

            ReadOnlySpan<Sector> sectors = Sectors;

            RenderWindowHelper.NewRender();

            Queue<NeighborsToRender> sectorRenderQueue = [];
            sectorRenderQueue.Enqueue(new NeighborsToRender
            {
                SectorId = player.Sector
            });

            int renderDepth = 0;

            do
            {
                NeighborsToRender sectorInfo = sectorRenderQueue.Dequeue();

                Sector sector = sectors[sectorInfo.SectorId];

                float yceil = sector.Ceil - pz;
                float yfloor = sector.Floor - pz;

                Span<Wall> walls = WallHelper.DetermineWallsToRender(sector,
                    sectorInfo.ParentWalls, pSin, pCos, px, py, yceil, yfloor, yaw);

                List<RenderableWall> neighbors = RenderSector(player, sector, sectors, sectorInfo, walls, screen, wallTexture);

                foreach (RenderableWall renderableWall in neighbors)
                {
                    Wall neighbor = renderableWall.Wall;

                    var fsdf = new NeighborsToRender
                    {
                        SectorId = neighbor.Neighbor,
                        RenderableWall = renderableWall,
                        ParentWalls =  { neighbor }
                    };
                    fsdf.ParentWalls.AddRange(sectorInfo.ParentWalls);

                    sectorRenderQueue.Enqueue(fsdf);
                }
            }
            while (sectorRenderQueue.Count > 0 || ++renderDepth >= EngineConstants.MaxPortalsRendered);
        }

        private List<RenderableWall> RenderSector(
            PortalPlayerSnapshot player,
            Sector sector,
            ReadOnlySpan<Sector> sectors,
            NeighborsToRender sectorInfo,
            Span<Wall> walls,
            Span<BGRA> screen,
            TextureInfo wallTexture)
        {
            GenerateDistanceCache(player, sector);
            RenderWindowHelper.NewSector(sectorInfo);

            List<RenderableWall> neightbors = [];

            TextureInfo groundTexture = TextureLoader.GetTexture(TextureName.CaveGround, false);
            TextureInfo ceilingTexture = TextureLoader.GetTexture(TextureName.CeilingOffice, false);

            List<RenderableWall> renderableWalls = [];

            for (int s = 0; s < walls.Length; s++)
            {
                Wall wall = walls[s];

                CalculateRenderWindow(wall, renderableWalls);
            }

            if (Vector.IsHardwareAccelerated)
            {
                RenderFloorVector2(player, sector, screen, groundTexture);
                RenderCeilingVector2(player, sector, screen, ceilingTexture);
            }

            for (int s = 0; s < renderableWalls.Count; s++)
            {
                RenderableWall renderableWall = renderableWalls[s];
                Wall wall = renderableWall.Wall;

                bool wallDrawn = wall.Neighbor == EngineConstants.NullSector ?
                    DrawBasicWall(screen, wallTexture, renderableWall) :
                    DrawPortalWall(sector, sectors, screen, wallTexture, renderableWall);

                if (wallDrawn && wall.Neighbor != EngineConstants.NullSector)
                {
                    neightbors.Add(renderableWall);
                }
            }

            return neightbors;
        }

        private void DebugPortal(
            Span<BGRA> screen,
            Span<RenderWindow> renderedArea)
        {
            int height = PixelWidth;

            for (int x = 0; x < PixelWidth; x++)
            {
                ref RenderWindow rendered = ref renderedArea[x];

                /*
                if (!rendered.Calculated)
                    continue;
                */
                Render(screen, rendered.CeilingStart, x, BGRA.Red);
                Render(screen, rendered.FloorEnd, x, BGRA.Blue);
                Render(screen, rendered.WallStart, x, BGRA.Green);
                Render(screen, rendered.WallEnd, x, BGRA.Yellow);

            }

            void Render(Span<BGRA> screen, int y, int x, BGRA color)
            {
                if (y != PixelHeight - 1)
                {
                    screen[(y + 1) * height + x] = color;

                }

                screen[y * height + x] = color;

                if (y != 0)
                {
                    screen[(y - 1) * height + x] = color;
                }
            }
        }

        private void CalculateRenderWindow(Wall wall, List<RenderableWall> renderableWalls)
        {
            Span<RenderWindow> renderedArea = RenderWindowHelper.RenderWindow;

            if (!RenderWindowHelper.SetWallToRender3(wall))
            {
                return;
            }

            (int offset, int wallFromX, int wallToX) = RenderWindowHelper.GetWallRenderWindowX();

            WallYPlaneInfo yPlaneInfo = WallHelper.CalculateLeftWallYPlaneInfo(wall, offset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            if (wallToX <= wallFromX)
            {
                return;
            }

            int renderableFromX = wallFromX;
            int renderableToX = wallToX;

            for (int x = wallFromX; x <= wallToX; x++)
            {
                int wallStartYInt = (int)wallStartY;
                int wallEndYInt = (int)wallEndY;

                ref RenderWindow renderedAreaX = ref renderedArea[x];

                if (renderedAreaX.Calculated || wallStartY >= wallEndY || renderedAreaX.CeilingStart >= renderedAreaX.FloorEnd)
                {

                    if (x - 1 > renderableFromX)
                    {
                        offset = wallFromX > wall.XLeft ? wallFromX - wall.XLeft : 0;

                        renderableWalls.Add(new RenderableWall
                        {
                            Wall = wall,
                            XLeft = renderableFromX,
                            XRight = x,
                            Offset = offset
                        });

                        renderableFromX = x;
                    }

                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                renderedAreaX.Calculated = true;
                renderedAreaX.WallStart = Math.Clamp(wallStartYInt, renderedAreaX.CeilingStart, renderedAreaX.FloorEnd);
                renderedAreaX.WallEnd = Math.Clamp(wallEndYInt, renderedAreaX.CeilingStart, renderedAreaX.FloorEnd);

                wallStartY += ceilDistIncr;
                wallEndY += floorDistIncr;
            }

            if (renderableToX > renderableFromX)
            {
                offset = renderableFromX > wall.XLeft ? renderableFromX - wall.XLeft : 0;

                renderableWalls.Add(new RenderableWall {
                    Wall = wall,
                    XLeft = renderableFromX,
                    XRight = renderableToX,
                    Offset = offset
                });
            }
        }


        private bool DrawPortalWall(
            Sector sector,
            ReadOnlySpan<Sector> sectors,
            Span<BGRA> screen,
            TextureInfo wallTexture,
            RenderableWall renderableWall)
        {
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            var wall = renderableWall.Wall;

            WallYPlaneInfo yPlaneInfo = WallHelper.CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);

            bool wallDrawn = false;


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

            if (floorOffset == 0 && ceilOffset == 0)
            {
                return true;
            }

            ref BGRA wallTexturePtr = ref wallTexture.Texture;
            ref uint screenPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(screen));

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                int wallStartYInt = (int)wallStartY;
                int wallEndYInt = (int)wallEndY;

                var result = RenderWindowHelper.TryGetRenderableDimensionsForX2(x, wallStartYInt, wallEndYInt);

                if (!result.Calculated)
                {
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                int portalFromY = result.CeilingStart; // wallStartYInt; // result.WallStart;
                int portalToY = result.FloorEnd;// wallEndYInt; //result.WallEnd;
                int clamptedFromY = Math.Max(result.WallStart, wallStartYInt);
                int clamptedToY = Math.Min(result.WallEnd, wallEndYInt);

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

                    for (int y = clamptedFromY; y < portalFromY; ++y)
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

                    for (int y = portalToY; y < clamptedToY; ++y)
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

                    ref var meh = ref RenderWindowHelper.RenderWindow[x];

                wallDrawn = true;// meh.CeilingStart != portalFromY || meh.FloorEnd != portalToY || meh.WallStart != portalFromY || meh.WallEnd != portalToY;

                meh.Calculated = false;
                meh.CeilingStart = portalFromY;
                meh.FloorEnd = portalToY;
                meh.WallStart = portalFromY;
                meh.WallEnd = portalToY;

                wallStartY += ceilDistIncr;
                wallEndY += floorDistIncr;
            }

            renderableWall.XLeft = wallFromX;
            renderableWall.XRight = wallToX;
            // wall.XLeft = wallFromX;
            // wall.XRight = wallToX;

            return wallDrawn;
        }

        private bool DrawBasicWall(
            Span<BGRA> screen,
            TextureInfo wallTexture,
            RenderableWall renderableWall)
        {
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;



            WallYPlaneInfo yPlaneInfo = WallHelper.CalculateLeftWallYPlaneInfo(renderableWall.Wall, wallFromXOffset);

            var wall = renderableWall.Wall;

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

                var result = RenderWindowHelper.TryGetRenderableDimensionsForX2(x, wallStartYInt, wallEndYInt);

                if (!result.Calculated)
                {
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                int clamptedFromY = result.WallStart;
                int clamptedToY = result.WallEnd;

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

                ref var meh = ref RenderWindowHelper.RenderWindow[x];
                meh.WallEnd = meh.WallStart;
                meh.FloorEnd = meh.WallStart;
                meh.Calculated = false;

                wallStartY += ceilDistIncr;
                wallEndY += floorDistIncr;
            }


            renderableWall.XLeft = wallFromX;
            renderableWall.XRight = wallToX;
            // wall.XLeft = wallFromX;
            // wall.XRight = wallToX;

            return true;
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
