namespace SoftwareRendererModels;

public sealed record TextureAngle(float Angle, GameTexture Texture, bool Flipped) : IFixedState;
