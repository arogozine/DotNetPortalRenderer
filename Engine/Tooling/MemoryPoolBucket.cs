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
        /// Temp Buffer
        /// </summary>
        Temp,
        /// <summary>
        /// Temp Buffer 2
        /// </summary>
        Temp2,
        /// <summary>
        /// Temp Buffer 3. Used by floor rendering so it can run concurrently with ceiling rendering (which uses Temp)
        /// </summary>
        Temp3,
        /// <summary>
        /// Temp Buffer 4. Used by floor rendering so it can run concurrently with ceiling rendering (which uses Temp2)
        /// </summary>
        Temp4,
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
