using BuildAssetLoader.Map;
using RenderingEngine.Engine;
using RenderingEngine.MapLoader;
using SoftwareRendererModels;
using System.Numerics;

namespace Tests
{
    public class SlopeCalculationTests
    {
        [Fact]
        public void SlopeGetsCalculatedProperly()
        {
            (RenderableSector slopedSector, RenderableSector upperSector) = Setup();

            var wallZero = slopedSector.Walls[0];
            wallZero.C1 = wallZero.R1 = wallZero.PointA;
            wallZero.C2 = wallZero.R2 = wallZero.PointB;

            var wallZero2 = slopedSector.Walls[0];
            wallZero2.C1 = wallZero2.R1 = wallZero2.PointA;
            wallZero2.C2 = wallZero2.R2 = wallZero2.PointB;

            var lowerCeilHeight = slopedSector.Ceil;
            var lowerFloorHeight = slopedSector.Floor;

            var upperCeilHeight = upperSector.Ceil;
            var upperFloorHeight = upperSector.Floor;

            Assert.True(slopedSector.Floor < upperSector.Floor);

            // at highest wall, slope calculation for slope sector matches the value for upper sector
            {
                RenderableWall touchingWall = slopedSector.Walls.Single(x => x.Neighbor == upperSector.Id);
                Vector2 pointA = touchingWall.PointA;
                Vector2 pointB = touchingWall.PointB;

                touchingWall.C1 = touchingWall.PointA;
                touchingWall.C2 = touchingWall.PointB;
                touchingWall.R1 = touchingWall.PointA;
                touchingWall.R2 = touchingWall.PointB;
                touchingWall.XLeft = 10;
                touchingWall.XRight = 110;

                (float FloorZA, float CeilingZA) = MathFormulas.CalculateZAtPoint(slopedSector, pointA);
                (float FloorZB, float CeilingZB) = MathFormulas.CalculateZAtPoint(slopedSector, pointB);

                Assert.Equal(CeilingZB, CeilingZA);
                Assert.Equal(upperCeilHeight, CeilingZA);
                Assert.Equal(float.Ceiling(FloorZA), float.Ceiling(FloorZB));
                Assert.Equal(upperFloorHeight, float.Ceiling(FloorZB));


                MathFormulas.FloorCeilSlope test = MathFormulas.CalculateFloorCeilingSlope(slopedSector, touchingWall, 0, false);
                Assert.Equal(0, test.CeilZIncrament);
                Assert.Equal(0, test.FloorZIncrament);
                Assert.Equal(upperFloorHeight, float.Ceiling(test.FloorZ));
                Assert.Equal(lowerCeilHeight, float.Ceiling(test.CeilZ));


                (float floorZ_a, float ceilingZ_a, float floorZ_b, float ceilingZ_b) = MathFormulas.CalculateSlopedFloorCeiling(slopedSector, touchingWall, true);
                Assert.Equal(floorZ_a, floorZ_b);
                Assert.Equal(ceilingZ_a, ceilingZ_b);

                Assert.Equal(upperFloorHeight, float.Ceiling(floorZ_a));
                Assert.Equal(upperFloorHeight, float.Ceiling(floorZ_b));
                Assert.Equal(lowerCeilHeight, float.Ceiling(ceilingZ_a));
                Assert.Equal(lowerCeilHeight, float.Ceiling(ceilingZ_b));
            }

            // at lowest wall, slope calculation for slope sector matches starting slope 
            {
                RenderableWall touchingWall = slopedSector.Walls.Single(x => x.Neighbor == 11);
                Vector2 pointA = touchingWall.PointA;
                Vector2 pointB = touchingWall.PointB;

                touchingWall.C1 = touchingWall.PointA;
                touchingWall.C2 = touchingWall.PointB;
                touchingWall.R1 = touchingWall.PointA;
                touchingWall.R2 = touchingWall.PointB;
                touchingWall.XLeft = 10;
                touchingWall.XRight = 110;

                (float FloorZA, float CeilingZA) = MathFormulas.CalculateZAtPoint(slopedSector, pointA);
                (float FloorZB, float CeilingZB) = MathFormulas.CalculateZAtPoint(slopedSector, pointB);

                Assert.Equal(CeilingZB, CeilingZA);
                Assert.Equal(lowerCeilHeight, CeilingZA);
                Assert.Equal(float.Ceiling(FloorZA), float.Ceiling(FloorZB));
                Assert.Equal(lowerFloorHeight, float.Ceiling(FloorZB));

                MathFormulas.FloorCeilSlope test = MathFormulas.CalculateFloorCeilingSlope(slopedSector, touchingWall, 0, false);
                Assert.Equal(0, test.CeilZIncrament);
                Assert.Equal(0, test.FloorZIncrament);
                Assert.Equal(lowerFloorHeight, float.Ceiling(test.FloorZ));
                Assert.Equal(lowerCeilHeight, float.Ceiling(test.CeilZ));

                (float floorZ_a, float ceilingZ_a, float floorZ_b, float ceilingZ_b) = MathFormulas.CalculateSlopedFloorCeiling(slopedSector, touchingWall, true);
                Assert.Equal(lowerFloorHeight, float.Ceiling(floorZ_a));
                Assert.Equal(lowerFloorHeight, float.Ceiling(floorZ_b));
                Assert.Equal(lowerCeilHeight, float.Ceiling(ceilingZ_a));
                Assert.Equal(lowerCeilHeight, float.Ceiling(ceilingZ_b));
            }

            {
                RenderableWall slopedWall = slopedSector.Walls.Single(x => x.Id == 52);

                Vector2 pointA = slopedWall.PointA;
                Vector2 pointB = slopedWall.PointB;

                slopedWall.C1 = slopedWall.PointA;
                slopedWall.C2 = slopedWall.PointB;
                slopedWall.XLeft = 10;
                slopedWall.XRight = 110;

                (float FloorZA, float CeilingZA) = MathFormulas.CalculateZAtPoint(slopedSector, pointA);
                (float FloorZB, float CeilingZB) = MathFormulas.CalculateZAtPoint(slopedSector, pointB);

                Assert.Equal(CeilingZB, CeilingZA);
                Assert.Equal(lowerCeilHeight, CeilingZA);
                Assert.NotEqual(float.Ceiling(FloorZA), float.Ceiling(FloorZB));
                Assert.Equal(upperFloorHeight, float.Ceiling(FloorZB));
                Assert.Equal(lowerFloorHeight, float.Ceiling(FloorZA));

                var test = MathFormulas.CalculateFloorCeilingSlope(slopedSector, slopedWall, 0, false);
                Assert.Equal(0, test.CeilZIncrament);
                Assert.NotEqual(0, test.FloorZIncrament);
                Assert.Equal(lowerFloorHeight, float.Ceiling(test.FloorZ));
                Assert.Equal(lowerCeilHeight, float.Ceiling(test.CeilZ));

                float floorZUpper = test.FloorZ + test.FloorZIncrament * (slopedWall.XRight - slopedWall.XLeft);
                Assert.Equal(upperFloorHeight, float.Ceiling(floorZUpper));

                (float floorZ_a, float ceilingZ_a, float floorZ_b, float ceilingZ_b) = MathFormulas.CalculateSlopedFloorCeiling(slopedSector, slopedWall, true);
                Assert.NotEqual(floorZ_a, floorZ_b);
                Assert.Equal(lowerCeilHeight, float.Ceiling(ceilingZ_a));
                Assert.Equal(lowerCeilHeight, float.Ceiling(ceilingZ_b));
            }
        }

        [Fact]
        public void SlopeGetsCalculatedProperly2()
        {
            RenderableSector slopedSector = SetupSloped();

            float ceilZ = slopedSector.Ceil;
            float floorZ = slopedSector.Floor;

            // 6208
            Vector2 pt1 = slopedSector.Walls[0].PointA;
            Vector2 pt2 = slopedSector.Walls[1].PointA;
            // 5792
            Vector2 pt3 = slopedSector.Walls[2].PointA;
            Vector2 pt4 = slopedSector.Walls[3].PointA;

            (float floorz1, float ceilingz1) = MathFormulas.CalculateZAtPoint(slopedSector, pt1);
            (float floorz2, float ceilingz2) = MathFormulas.CalculateZAtPoint(slopedSector, pt2);

            Assert.Equal(floorz1, floorz2);
            Assert.Equal(ceilingz1, ceilingz2);
            Assert.Equal(floorz1, ceilZ);
            Assert.Equal(ceilingz1, floorZ);

            (float floorz3, float ceilingz3) = MathFormulas.CalculateZAtPoint(slopedSector, pt3);
            (float floorz4, float ceilingz4) = MathFormulas.CalculateZAtPoint(slopedSector, pt4);

            Assert.Equal(floorz3, floorz4);
            Assert.Equal(ceilingz3, ceilingz4);
        }

        internal static RenderableSector SetupSloped()
        {
            SectorType sector308 = new SectorType(
                ceilingHeiNum: 2560,
                ceilingPal: 0,
                ceilingPicNum: 815,
                ceilingShade: 23,
                ceilingStat: Stat.Sloped | Stat.DoubleSmooshiness | Stat.AlignTexture,
                ceilingXPanning: 0,
                ceilingYPanning: 0,
                ceilingZ: -113664,
                extra: -1,
                filler: 0,
                floorHeiNum: 2560,
                floorPal: 0,
                floorPicNum: 815,
                floorShade: 23,
                floorStat: Stat.Sloped | Stat.DoubleSmooshiness | Stat.AlignTexture,
                floorXPanning: 0,
                floorYPanning: 0,
                floorZ: -113664,
                hiTag: 0,
                loTag: 0,
                visibility: 0,
                wallNum: 4,
                wallPtr: 2025
            );

            WallType wall2025 = new WallType(
                cStat: WallCStat.AlignPictureOnBottom | WallCStat.XFlipped,
                extra: -1,
                hiTag: 0,
                loTag: 0,
                nextSector: -1,
                nextWall: -1,
                overPicNum: 0,
                pal: 0,
                picNum: 750,
                point2: 2026,
                shade: 6,
                x: 34304,
                xPanning: 0,
                xRepeat: 9,
                y: -49664,
                yPanning: 0,
                yRepeat: 18
            );

            WallType wall2026 = new WallType(
                cStat: WallCStat.AlignPictureOnBottom | WallCStat.XFlipped,
                extra: -1,
                hiTag: 0,
                loTag: 0,
                nextSector: -1,
                nextWall: -1,
                overPicNum: 0,
                pal: 0,
                picNum: 750,
                point2: 2027,
                shade: 6,
                x: 34816,
                xPanning: 0,
                xRepeat: 58,
                y: -49664,
                yPanning: 0,
                yRepeat: 18
            );

            WallType wall2027 = new WallType(
                cStat: WallCStat.AlignPictureOnBottom | WallCStat.XFlipped,
                extra: -1,
                hiTag: 0,
                loTag: 0,
                nextSector: -1,
                nextWall: -1,
                overPicNum: 0,
                pal: 0,
                picNum: 750,
                point2: 2028,
                shade: 6,
                x: 34816,
                xPanning: 0,
                xRepeat: 9,
                y: -46336,
                yPanning: 0,
                yRepeat: 18
            );

            WallType wall2028 = new WallType(
                cStat: default,
                extra: -1,
                hiTag: 0,
                loTag: 0,
                nextSector: 307,
                nextWall: 2024,
                overPicNum: 0,
                pal: 0,
                picNum: 3387,
                point2: 2025,
                shade: 20,
                x: 34304,
                xPanning: 0,
                xRepeat: 26,
                y: -46336,
                yPanning: 246,
                yRepeat: 8
            );


            MapSector slopedSector = GrpReader.ParseSectorType(10, in sector308);
            slopedSector.Walls.Add(ParseWallType(2025, 2025, in wall2025, in wall2026, in wall2026));
            slopedSector.Walls.Add(ParseWallType(2026, 2026, in wall2026, in wall2027, in wall2027));
            slopedSector.Walls.Add(ParseWallType(2027, 2027, in wall2027, in wall2028, in wall2028));
            slopedSector.Walls.Add(ParseWallType(2028, 2028, in wall2028, in wall2025, in wall2025));

            return GameRenderStateLoader.ParseMapSector(slopedSector);
        }

        internal static (RenderableSector slopedSector, RenderableSector upperSector) Setup()
        {
            SectorType sector10 = new SectorType(
                ceilingHeiNum: 0,
                ceilingPal: 0,
                ceilingPicNum: 89,
                ceilingShade: 9,
                ceilingStat: Stat.Parallaxing,
                ceilingXPanning: 0,
                ceilingYPanning: 0,
                ceilingZ: -113664,
                extra: -1,
                filler: 0,
                floorHeiNum: 0,
                floorPal: 0,
                floorPicNum: 876,
                floorShade: 8,
                floorStat: Stat.SwapXy | Stat.DoubleSmooshiness | Stat.XFlip,
                floorXPanning: 0,
                floorYPanning: 0,
                floorZ: -26624,
                hiTag: 0,
                loTag: 0,
                visibility: 240,
                wallNum: 5,
                wallPtr: 41
            );

            SectorType sector12 = new SectorType(
                ceilingHeiNum: 0,
                ceilingPal: 0,
                ceilingPicNum: 89,
                ceilingShade: 9,
                ceilingStat: Stat.Parallaxing,
                ceilingXPanning: 0,
                ceilingYPanning: 0,
                ceilingZ: -113664,
                extra: -1,
                filler: 0,
                floorHeiNum: -1243,
                floorPal: 0,
                floorPicNum: 876,
                floorShade: 11,
                floorStat: Stat.Sloped | Stat.SwapXy | Stat.DoubleSmooshiness | Stat.XFlip,
                floorXPanning: 0,
                floorYPanning: 0,
                floorZ: -9216,
                hiTag: 0,
                loTag: 0,
                visibility: 240,
                wallNum: 6,
                wallPtr: 51);

            WallType wall51 = new WallType(
                cStat: default,
                extra: -1,
                hiTag: 0,
                loTag: 0,
                nextSector: 11,
                nextWall: 50,
                overPicNum: 0,
                pal: 0,
                picNum: 745,
                point2: 52,
                shade: 7,
                x: 29184,
                xPanning: 0,
                xRepeat: 14,
                y: -33024,
                yPanning: 0,
                yRepeat: 8
            );

            WallType wall52 = new WallType(
                cStat: default,
                extra: -1,
                hiTag: 0,
                loTag: 0,
                nextSector: -1,
                nextWall: -1,
                overPicNum: 0,
                pal: 0,
                picNum: 746,
                point2: 53,
                shade: 16,
                x: 29184,
                xPanning: 1,
                xRepeat: 24,
                y: -35072,
                yPanning: 0,
                yRepeat: 8
            );

            WallType wall53 = new WallType(
                cStat: default,
                extra: -1,
                hiTag: 0,
                loTag: 0,
                nextSector: 10,
                nextWall: 45,
                overPicNum: 0,
                pal: 0,
                picNum: 745,
                point2: 54,
                shade: 7,
                x: 32768,
                xPanning: 0,
                xRepeat: 10,
                y: -35072,
                yPanning: 0,
                yRepeat: 8
            );

            WallType wall54 = new WallType(
                cStat: default,
                extra: -1,
                hiTag: 0,
                loTag: 0,
                nextSector: 129,
                nextWall: 816,
                overPicNum: 0,
                pal: 0,
                picNum: 3387,
                point2: 55,
                shade: 12,
                x: 32768,
                xPanning: 24,
                xRepeat: 1,
                y: -33024,
                yPanning: 0,
                yRepeat: 8
            );

            WallType wall55 = new WallType(
                cStat: default,
                extra: -1,
                hiTag: 0,
                loTag: 0,
                nextSector: -1,
                nextWall: -1,
                overPicNum: 0,
                pal: 0,
                picNum: 745,
                point2: 56,
                shade: -4,
                x: 32640,
                xPanning: 24,
                xRepeat: 4,
                y: -33024,
                yPanning: 0,
                yRepeat: 8
            );

            WallType wall56 = new WallType(
                cStat: default,
                extra: -1,
                hiTag: 0,
                loTag: 0,
                nextSector: 270,
                nextWall: 1727,
                overPicNum: 0,
                pal: 0,
                picNum: 746,
                point2: 51,
                shade: 2,
                x: 32128,
                xPanning: 24,
                xRepeat: 23,
                y: -33024,
                yPanning: 0,
                yRepeat: 8
            );

            MapSector upperSector = GrpReader.ParseSectorType(10, in sector10);
            MapSector slopedSector = GrpReader.ParseSectorType(12, in sector12);

            slopedSector.Walls.Add(ParseWallType(51, 51, in wall51, in wall52, in wall52));
            slopedSector.Walls.Add(ParseWallType(52, 52, in wall52, in wall53, in wall53));
            slopedSector.Walls.Add(ParseWallType(53, 53, in wall53, in wall54, in wall54));
            slopedSector.Walls.Add(ParseWallType(54, 54, in wall54, in wall55, in wall55));
            slopedSector.Walls.Add(ParseWallType(55, 55, in wall55, in wall56, in wall56));
            slopedSector.Walls.Add(ParseWallType(56, 51, in wall56, in wall51, in wall51));

            return (GameRenderStateLoader.ParseMapSector(slopedSector), GameRenderStateLoader.ParseMapSector(upperSector));
        }

        internal static Line ParseWallType(int index, int j, in WallType wall, in WallType nextWall, in WallType point2Wall)
        {
            return new Line
            {
                Id = index,
                PointA = new LineVector(j, GrpReader.GetPoint(in wall)),
                PointB = new LineVector(wall.Point2, GrpReader.GetPoint(in point2Wall)),
                SectorTo = wall.NextSector,
                UpperTexture = GrpReader.GetTextureInfo(in wall, in wall, false),
                MiddleTexture = GrpReader.GetTextureInfo(in wall, in wall, true),
                LowerTexture = GrpReader.GetTextureInfo(in wall, in nextWall, false),
                UpperShade = wall.Shade
            };
        }
    }
}