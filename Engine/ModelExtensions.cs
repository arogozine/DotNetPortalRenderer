using RenderingEngine.Engine;
using SoftwareRendererModels;
using System.Numerics;

namespace RenderingEngine;

internal static class ModelExtensions
{
    extension (Vector2 vector)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out float item1, out float item2)
        {
            item1 = vector.X;
            item2 = vector.Y;
        }
    }

    extension (MapSectorSettings sectorSettings)
    {
        public bool Sloped => (sectorSettings & MapSectorSettings.SlopeCeiling | MapSectorSettings.SlopeFloor) != MapSectorSettings.None;
    }

    extension(TextureRenderingOptions options)
    {
        public bool IsSkybox => options.HasFlag(TextureRenderingOptions.Skybox);
        public bool IsFlippedX => options.HasFlag(TextureRenderingOptions.FlipX);
        public bool IsFlippedY => options.HasFlag(TextureRenderingOptions.FlipY);
        public bool IsSwappedXY => options.HasFlag(TextureRenderingOptions.SwapXY);
        public bool IsAlignedWithWall => options.HasFlag(TextureRenderingOptions.AlignWithFirstWall);
    }

    extension(GameTextureInfo gameTextureInfo)
    {
        public (float Width, float Height) GetScaledDemensions()
        {
            (float xScale, float yScale) = gameTextureInfo.GetScale();

            return (gameTextureInfo.Width * xScale, gameTextureInfo.Height * yScale);
        }

        public (float XScale, float YScale) GetScale()
        {
            return (gameTextureInfo.XScale ?? 1f, gameTextureInfo.YScale ?? 1f);
        }
    }

    extension(GameTexture gameTexture)
    {
        public BGRA[] CalculateTexture(int palletteId)
        {
            if (gameTexture is BuildTexture buildTexture)
            {
                return TextureCache.GetTexture(buildTexture.Lookup, palletteId);
            }
            else if (gameTexture is DoomTexture doomTexture)
            {
                BGRA[] texture = new BGRA[doomTexture.Texture.Length];
                doomTexture.Texture.AsSpan().CopyTo(texture);

                TextureTransformHelper.ShadeInPlace(texture, palletteId);

                return texture;
            }

            throw new NotSupportedException();
        }

        public ref T GetBinaryRef<T>(int shade, TextureTransform transform)
            where T : unmanaged
        {
            Span<T> span = MemoryMarshal.Cast<BGRA, T>(GetBinary(gameTexture, shade, transform));
            return ref MemoryMarshal.GetReference(span);
        }

        private BGRA[] GetOrAddNormal(int shade)
        {
            Dictionary<int, BGRA[]> pallette = gameTexture.TransformToPallette[(int)TextureTransform.Normal];

            if (pallette.TryGetValue(shade, out BGRA[]? value))
            {
                return value;
            }

            value = CalculateTexture(gameTexture, shade);
            pallette[shade] = value;

            return value;
        }

        private Span<BGRA> GetBinary(int shade, TextureTransform transform)
        {
            Dictionary<int, BGRA[]> pallette = gameTexture.TransformToPallette[(int)transform];

            if (pallette.TryGetValue(shade, out BGRA[]? texture))
            {
                return texture;
            }

            texture = GetOrAddNormal(gameTexture, shade);

            if (transform == TextureTransform.Normal)
            {
                pallette[shade] = texture;
                return texture;
            }

            if (transform.HasFlag(TextureTransform.FlippedX))
            {
                texture = TextureTransformHelper.FlipTextureX(gameTexture.Height, gameTexture.Width, texture);
            }

            if (transform.HasFlag(TextureTransform.FlippedY))
            {
                texture = TextureTransformHelper.FlipTextureY(gameTexture.Height, gameTexture.Width, texture);
            }

            if (transform.HasFlag(TextureTransform.Rotated))
            {
                texture = TextureTransformHelper.RotateTexture(gameTexture.Height, gameTexture.Width, texture);
            }

            pallette[shade] = texture;
            return texture;
        }
    }
}
