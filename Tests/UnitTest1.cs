using RenderingEngine.Engine;
using RenderingEngine.Models;

namespace Tests
{
    public class UnitTest1
    {
        [Fact]
        public void FilterOutWallsBehindPlayer_Works()
        {
            Span<Wall> walls = [new Wall(null!, 1f, 1f, 2f, 2f, null)];

            WallHelper.FilterOutWallsBehindPlayer(ref walls);

            Assert.NotEmpty(walls.ToArray());
        }

        [Fact]
        public void BreakUpIntoBunches_SingleWall_Works()
        {
            Span<Wall> walls = [new Wall(null!, 1f, 1f, 2f, 2f, null)];

            Span<Range> bunches = WallHelper.BreakUpIntoBunches(walls);

            for (int i = 0; i < bunches.Length; i++) {
                Span<Wall> bunch = walls[bunches[i]];
                Assert.NotEmpty(bunch.ToArray());
            }

            Assert.Single(bunches.ToArray());
        }

        [Fact]
        public void BreakUpIntoBunches_TwoSquares_Works()
        {
            Span<Wall> walls = [
                // Square A
                new Wall(null!, 0f, 0f, 5f, 0f, null),
                new Wall(null!, 5f, 0f, 5f, 5f, null),
                new Wall(null!, 5f, 5f, 0f, 5f, null),
                new Wall(null!, 0f, 5f, 0f, 0f, null),
                // Square B
                new Wall(null!, 1f, 1f, 2f, 1f, null),
                new Wall(null!, 2f, 1f, 2f, 2f, null),
                new Wall(null!, 2f, 2f, 1f, 2f, null),
                new Wall(null!, 1f, 2f, 1f, 1f, null),
            ];

            Span<Range> bunches = WallHelper.BreakUpIntoBunches(walls);

            Assert.Equal(2, bunches.Length);

            List<Wall> wallList = [];
            for (int i = 0; i < bunches.Length; i++)
            {
                Span<Wall> bunch = walls[bunches[i]];
                Assert.Equal(4, bunch.Length);

                wallList.AddRange(walls[bunches[i]]);
            }

            wallList = [.. wallList.Distinct()];

            Assert.Equal(walls.Length, wallList.Count);
        }

        [Fact]
        public void BreakUpIntoBunches_TwoSquaresOneTriangle_Works()
        {
            Span<Wall> walls = [
                // Square A
                new Wall(null!, 0f, 0f, 5f, 0f, null),
                new Wall(null!, 5f, 0f, 5f, 5f, null),
                new Wall(null!, 5f, 5f, 0f, 5f, null),
                new Wall(null!, 0f, 5f, 0f, 0f, null),
                // Square B
                new Wall(null!, 1f, 1f, 2f, 1f, null),
                new Wall(null!, 2f, 1f, 2f, 2f, null),
                new Wall(null!, 2f, 2f, 1f, 2f, null),
                new Wall(null!, 1f, 2f, 1f, 1f, null),
                // Triangle
                new Wall(null!, 0f, 0.33f, 0.33f, 0.66f, null),
                new Wall(null!, 0.33f, 0.66f, -0.33f, 0.66f, null),
                new Wall(null!, -0.33f, 0.66f, 0f, 0.33f, null)
            ];

            Span<Range> bunches = WallHelper.BreakUpIntoBunches(walls);

            Assert.Equal(3, bunches.Length);

            List<Wall> wallList = [];
            for (int i = 0; i < bunches.Length; i++)
            {
                Span<Wall> bunch = walls[bunches[i]];
                Assert.Equal(i == 2 ? 3 : 4, bunch.Length);

                wallList.AddRange(walls[bunches[i]]);
            }

            wallList = [.. wallList.Distinct()];

            Assert.Equal(walls.Length, wallList.Count);
        }
    }
}
