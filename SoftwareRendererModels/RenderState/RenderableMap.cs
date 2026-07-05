using System.Diagnostics;

namespace SoftwareRendererModels;

[DebuggerDisplay("Sectors = {Sectors.Length}, Sprites = {Sprites.Length}")]
public sealed record RenderableMap(RenderableSector[] Sectors, RenderableSprite[] Sprites) : IFixedState;
