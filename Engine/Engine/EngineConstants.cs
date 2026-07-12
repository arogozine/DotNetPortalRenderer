namespace RenderingEngine.Engine
{
    internal static class EngineConstants
    {
        // Maximum amount of iterations to render
        public const int MaxRenderDepth = 2048;
        
        // This is DoomGuy height, but as I am working with rendering doom maps at the moment, is a good starting point
        public const int PlayerHeight = 64;// 56;

        public const float CameraPlaneX = 1f;
        public const float HeightToWidthRatio = 0.5f / CameraPlaneX;
    }
}
