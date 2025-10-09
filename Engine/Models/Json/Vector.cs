namespace RenderingEngine.Models.Json
{
    public sealed record Vector(float X, float Y)
    {
        public static implicit operator Vector((float, float) tuple)
        {
            return new Vector(tuple.Item1, tuple.Item2);
        }
    }
}
