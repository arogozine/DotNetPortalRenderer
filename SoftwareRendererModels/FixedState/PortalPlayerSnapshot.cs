using SoftwareRendererModels;

namespace SoftwareRendererModels;

public sealed class PortalPlayerSnapshot : IFixedState
{
    public int Sector { get; }
    public float Angle { get; }
    public float Sin { get; }
    public float Cos { get; }
    public float X { get; }
    public float Y { get; }
    public float Z { get; }
    public XyzTuple Velocity { get; }
    public float Yaw { get; }

    public PortalPlayerSnapshot((float px, float py, float pz) where, XyzTuple velocity, float angle, float yaw, int sector)
    {
        (X, Y, Z) = where;
        (Sin, Cos) = MathF.SinCos(angle);
        Angle = angle;
        Velocity = velocity;
        Yaw = yaw;
        Sector = sector;
    }
}
