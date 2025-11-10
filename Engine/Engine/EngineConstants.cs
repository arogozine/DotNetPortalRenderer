namespace RenderingEngine.Engine
{
    internal static class EngineConstants
    {
        public const int MaxPortalsRendered = 1024;

        public const int NullSector = -1;
        public const int Unset = -1;

        public const int MaxSectorRenderQueue = 32;
        public const float CameraPlaneX = 0.66f;
        public const float LightFallOffDistance = 1024f;
        public const float OneOverLightFallOffDistance = 1f / LightFallOffDistance;

        // This is DoomGuy height, but as I am working with rendering doom maps at the moment, is a good starting point
        public const int PlayerHeight = 56;

        public const float HeightToWidthRatio = 0.5f / CameraPlaneX;
    }
}
