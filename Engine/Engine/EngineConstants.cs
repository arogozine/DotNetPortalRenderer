namespace RenderingEngine.Engine
{
    internal static class EngineConstants
    {
        public const int MaxPortalsRendered = 1024;

        public const int NullSector = -1;
        public const int Unset = -1;

        public const int MaxSectorRenderQueue = 32;
        public const float CameraPlaneX = 0.66f;
        public const float LightFallOffDistance = 256f;
        public const float OneOverLightFallOffDistance = 1f / LightFallOffDistance;
    }
}
