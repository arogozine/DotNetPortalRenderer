namespace RenderingEngine.Engine
{
    internal static class EngineConstants
    {
        // Maximum amount of iterations to render
        public const int MaxRenderDepth = 2048;

        public const int NullSector = -1;
        public const int Unset = -1;

        // This is DoomGuy height, but as I am working with rendering doom maps at the moment, is a good starting point
        public const int PlayerHeight = 64;// 56;

        public const float CameraPlaneX = 1f;
        public const float HeightToWidthRatio = 0.5f / CameraPlaneX;

        public const float NinetyDegrees = MathF.PI * (90f / 180f);

        // AI Assisted: minimum screen-column span for a sector before ceiling/floor or wall
        // rendering is worth handing off to the concurrent worker thread; below this, run
        // inline to avoid signal/wait overhead exceeding the cost of the work itself.
        public const int MinParallelSectorColumns = 128;
    }
}
