using SoftwareRendererModels;

namespace RenderingEngine.Engine;

[SkipLocalsInit]
internal static class TextureCache
{
    private const string FallBack = "-";
    private static readonly Dictionary<string, GameTexture> Cache = [];
    private static readonly Dictionary<int, BGRA[]> PalletteLookup = [];

    public static IEnumerable<string> TextureNames => Cache.Keys;

    static TextureCache()
    {
        var data = new BGRA[128 * 128];
        data.AsSpan().Fill(BGRA.Green);

        Cache[FallBack] = new DoomTexture(FallBack, 128, 128, data);
    }

    public static void Add(string name, GameTexture texture)
    {
        name = name.ToUpperInvariant();
        Cache[name] = texture;
    }

    public static bool TextureExists(string name)
    {
        name = name.ToUpperInvariant();
        return Cache.ContainsKey(name);
    }

    public static GameTexture GetTexture(GameTextureInfo? textureInfo) => GetTexture(textureInfo?.Name);

    public static GameTexture GetTexture(string? name)
    {
        if (name == null)
        {
            return Cache[FallBack];
        }

        name = name.ToUpperInvariant();

        if (!Cache.TryGetValue(name, out GameTexture? texture))
        {
            texture = Cache[FallBack];
        }

        return texture;
    }

    public static void AddPallette(int id, BGRA[] lookup)
    {
        PalletteLookup[id] = lookup;
    }

    public static BGRA[] GetTexture(ReadOnlySpan<byte> lookup, int palletteId)
    {
        if (!PalletteLookup.ContainsKey(palletteId))
        {
            palletteId = 0;
        }

        ReadOnlySpan<BGRA> pallette = PalletteLookup[palletteId];
        BGRA[] texture = new BGRA[lookup.Length];

        for (int i = 0; i < lookup.Length; i++)
        {
            byte index = lookup[i];
            texture[i] = pallette[index];
        }

        return texture;
    }
}