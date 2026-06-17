namespace SoftwareRendererModels;

public sealed class PortalPlayerSnapshot : IFixedState
{
    public int Sector { get; set; }
    public float Angle { get; set; }
    public float Sin { get; set; }
    public float Cos { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public XyzTuple Velocity { get; set; }
    public float Yaw { get; set; }
}
