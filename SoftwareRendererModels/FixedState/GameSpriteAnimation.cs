namespace SoftwareRendererModels;

public sealed class GameSpriteAnimation : IFixedState
{
    public required TextureAngle[][] AnimationToAngleToTexture { get; init; }

    public TextureAngle[] this[int index]
    {
        get => AnimationToAngleToTexture[index];
    }
}
