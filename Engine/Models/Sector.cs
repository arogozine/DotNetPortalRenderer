namespace RenderingEngine.Models
{
    internal class Sector
    {
        public required float Floor { get; init; }
        public required float Ceil { get; init; }
        public required Wall[] Walls { get; init; }
    }
}
