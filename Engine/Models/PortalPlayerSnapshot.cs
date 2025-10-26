namespace RenderingEngine.Models
{
    internal sealed class PortalPlayerSnapshot
    {
        public readonly int Sector;
        public readonly float Angle;
        public readonly float Sin;
        public readonly float Cos;
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly XyzTuple Velocity;
        public readonly float Yaw;

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
}
