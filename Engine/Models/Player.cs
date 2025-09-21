namespace RenderingEngine.Models
{
    public class Player
    {
        public XyzTuple Where { get; set; }
        public XyzTuple Velocity { get; set; }
        public float Angle { get; set; }
        public float Yaw { get; set; }
        public int Sector { get; set; }
    }
}
