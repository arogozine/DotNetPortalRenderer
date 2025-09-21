namespace RenderingEngine.Models
{
    internal sealed class PortalPlayerSnapshot
    {
        public PortalPlayerSnapshot(XyzTuple where, XyzTuple velocity, float angle, float yaw, int sector)
        {
            Where = where;
            Velocity = velocity;
            Angle = angle;
            Yaw = yaw;
            Sector = sector;
        }

        public XyzTuple Where { get; private set; }
        public XyzTuple Velocity { get; private set; }
        public float Angle { get; private set; }
        public float Yaw { get; private set; }
        public int Sector { get; private set; }
    }
}
