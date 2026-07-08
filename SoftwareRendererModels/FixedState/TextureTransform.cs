namespace SoftwareRendererModels;

[Flags, FixedState]
public enum TextureTransform : byte
{
    /// <summary>
    /// Normal
    /// </summary>
    Normal = 0,
    
    /// <summary>
    /// 90 degree rotated
    /// </summary>
    Rotated = 1,
    
    /// <summary>
    /// Texture should be flipped horizontally
    /// </summary>
    FlippedY = 2,
    
    /// <summary>
    /// Texture should be flipped vertically
    /// </summary>
    FlippedX = 4,
    
    /// <summary>
    /// All cached transforms
    /// </summary>
    All = Rotated | FlippedX | FlippedY
}