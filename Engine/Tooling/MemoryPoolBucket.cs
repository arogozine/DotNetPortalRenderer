namespace RenderingEngine.Tooling
{
    internal enum MemoryPoolBucket
    {
        /// <summary>
        /// Generated once per window size
        /// </summary>
        AngleCache,
        /// <summary>
        /// Generated once per window size
        /// </summary>
        CameraHeightToMapYPos,
        /// <summary>
        /// Generated once per window size
        /// </summary>
        XMapPosMultiplierCache,
        /// <summary>
        /// Rendering / rendered status of each column
        /// </summary>
        RenderColumnStatus,
        /// <summary>
        /// Portal Start (Ceiling is rendered between CeilingStart - WallStart)
        /// </summary>
        CeilingStart,
        /// <summary>
        /// WallStart clamped to [CeilingStart, FloorEnd]
        /// </summary>
        WallStartClamped,
        /// <summary>
        /// WallEnd clamped to [CeilingStart, FloorEnd]
        /// </summary>
        WallEndClamped,
        /// <summary>
        /// Portal End (Floor is rendered between WallEnd - FloorEnd)
        /// </summary>
        FloorEnd,
        /// <summary>
        /// Z Distance for Sprite Rendering
        /// </summary>
        Distance,
        /// <summary>
        /// Texture Column to Render
        /// </summary>
        TextureXLocation,
        /// <summary>
        /// Wall Texture Y Increment
        /// </summary>
        TextureYIncrement,
        /// <summary>
        /// Where to start rendering a wall
        /// </summary>
        WallStart,
        /// <summary>
        /// Where to end rendering a wall
        /// </summary>
        WallEnd,
        /// <summary>
        /// Portal Top Window. Between [WallStart, WallEnd]
        /// </summary>
        PortalFrom,
        /// <summary>
        /// Portal Bottom Window. Between [WallStart, WallEnd]
        /// </summary>
        PortalTo,
        /// <summary>
        /// Calculated Texture Horizontal Position
        /// </summary>
        StartingYTexturePosition,
        /// <summary>
        /// PortalFrom Clamped Between [WallStartClamped, WallEndClamped]
        /// </summary>
        PortalFromClamped,
        /// <summary>
        /// PortalTo Clamped Between [WallStartClamped, WallEndClamped]
        /// </summary>
        PortalToClamped,
        /// <summary>
        /// Screen Buffer
        /// </summary>
        Buffer
    }
}
