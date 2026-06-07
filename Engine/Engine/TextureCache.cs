using SoftwareRendererModels;

namespace RenderingEngine.Engine;

[SkipLocalsInit]
internal static class TextureCache
{
    private const string FallBack = "-";
    private static readonly Dictionary<string, GameTexture> TextureNameLookup = [];
    private static readonly Dictionary<int, Dictionary<int, BGRA[]>> PaletteToShadeLookup = [];

    public static IEnumerable<string> TextureNames => TextureNameLookup.Keys;

    static TextureCache()
    {
        var data = new BGRA[128 * 128];
        data.AsSpan().Fill(BGRA.Green);

        TextureNameLookup[FallBack] = new DoomTexture(FallBack, 128, 128, data);
    }

    public static void Add(string name, GameTexture texture)
    {
        name = name.ToUpperInvariant();
        TextureNameLookup[name] = texture;
    }

    public static bool TextureExists(string name)
    {
        name = name.ToUpperInvariant();
        return TextureNameLookup.ContainsKey(name);
    }

    public static bool HasTexture(string? name)
    {
        if (name == null)
        {
            return false;
        }

        name = name.ToUpperInvariant();

        return TextureNameLookup.ContainsKey(name);
    }

    public static GameTexture GetTexture(GameTextureInfo? textureInfo) => GetTexture(textureInfo?.Name);

    public static GameTexture GetTexture(string? name)
    {
        if (name == null)
        {
            return TextureNameLookup[FallBack];
        }

        name = name.ToUpperInvariant();

        if (!TextureNameLookup.TryGetValue(name, out GameTexture? texture))
        {
            texture = TextureNameLookup[FallBack];
        }

        return texture;
    }

    public static void AddPallette(int id, BGRA[] lookup)
    {
        AddPallette(0, id, lookup);
    }

    public static void AddPallette(int id, int shade, BGRA[] lookup)
    {
        if (!PaletteToShadeLookup.TryGetValue(id, out Dictionary<int, BGRA[]>? shadeLookup))
        {
            shadeLookup = [];
            PaletteToShadeLookup[id] = shadeLookup;
        }

        shadeLookup[shade] = lookup;
    }

    public static BGRA[] GetTexture(ReadOnlySpan<byte> lookup, int palletteId, int shade)
    {
        if (!PaletteToShadeLookup.ContainsKey(palletteId))
        {
            palletteId = 0;
        }

        Dictionary<int, BGRA[]>? shadeLookup = PaletteToShadeLookup[palletteId];


        if (!shadeLookup.ContainsKey(shade))
        {
            shade = 0;
        }

        ReadOnlySpan <BGRA> pallette = shadeLookup[shade];

        BGRA[] texture = new BGRA[lookup.Length];

        for (int i = 0; i < lookup.Length; i++)
        {
            byte index = lookup[i];
            texture[i] = pallette[index];
        }

        return texture;
    }
}