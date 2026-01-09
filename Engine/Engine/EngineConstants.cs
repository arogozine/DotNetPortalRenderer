namespace RenderingEngine.Engine
{
    internal static class EngineConstants
    {
        // Maximum amount of iterations to render
        public const int MaxRenderDepth = 2048;

        public const int NullSector = -1;
        public const int Unset = -1;

        // This is DoomGuy height, but as I am working with rendering doom maps at the moment, is a good starting point
        public const int PlayerHeight = 56;

        // Build and Doom, afaik, use 90degrees - so this can be simplified
        public const float CameraPlaneX = 1f;
        public const float HeightToWidthRatio = 0.5f / CameraPlaneX;

        public const float NinetyDegrees = MathF.PI * (90f / 180f);
    }
}
