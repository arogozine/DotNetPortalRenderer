namespace RenderingEngine.Models;

internal sealed record LineVector(int Id, Point Point)
{
    public float X => Point.X;
    public float Y => Point.Y;

    public static implicit operator Point(LineVector value) => value.Point;
}
