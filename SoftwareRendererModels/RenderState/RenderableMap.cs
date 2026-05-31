namespace SoftwareRendererModels;

public sealed record RenderableMap(RenderableSector[] Sectors, RenderableSprite[] Sprites) : IFixedState;
