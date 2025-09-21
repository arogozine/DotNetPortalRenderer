namespace RenderingEngine.Engine
{
    internal struct RenderedArea
    {
        public bool Calculated;
        public int From;
        public int To;

        internal readonly void Deconstruct(out bool calculated, out int from, out int to)
        {
            calculated = this.Calculated;
            to = this.To;
            from = this.From;
        }

        public static implicit operator RenderedArea((int From, int To) v)
        {
            return new RenderedArea
            {
                Calculated = true,
                From = v.From,
                To = v.To
            };
        }

        public override readonly string ToString()
        {
            return Calculated ? $"({From}, {To})" : "None";
        }
    }
}
