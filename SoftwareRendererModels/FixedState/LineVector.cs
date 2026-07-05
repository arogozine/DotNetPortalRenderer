using System.Diagnostics;
using System.Numerics;

namespace SoftwareRendererModels;

[DebuggerDisplay("Id = {Id}, X = {X}, Y = {Y}")]
public sealed record LineVector(int Id, Vector2 Point) : IFixedState
{
    public float X => Point.X;
    public float Y => Point.Y;

    public static implicit operator Vector2(LineVector value) => value.Point;
}
