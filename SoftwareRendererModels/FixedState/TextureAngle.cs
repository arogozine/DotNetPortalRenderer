using System.Diagnostics;

namespace SoftwareRendererModels;

[DebuggerDisplay("Angle = {Angle}, Texture = {Texture.Name}")]
public sealed record TextureAngle(float Angle, GameTexture Texture, bool Flipped) : IFixedState;
