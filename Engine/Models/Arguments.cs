namespace RenderingEngine.Models
{
    public sealed class Arguments
    {
        // IWad is optional when using Palette/Grp mode
        public string? IWad { get; set; }
        public string? PWad { get; set; }
        public required string Map { get; set; }

        // Optional alternative resources: palette and grp (when using custom assets instead of IWAD/PWAD)
        public string? Palette { get; set; }
        public string? Grp { get; set; }

    }
}
