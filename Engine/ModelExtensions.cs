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
        public bool Sloped => (sectorSettings & (MapSectorSettings.SlopeCeiling | MapSectorSettings.SlopeFloor)) != MapSectorSettings.None; // AI Assisted: fixed operator-precedence bug (& binds tighter than |)
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

    private static readonly Lock _textureLock = new();

    extension(GameTexture gameTexture)
    {
        public ref T GetBinaryRef<T>(int id, int shade, TextureTransform transform)
            where T : unmanaged
        {
            Span<BGRA> binary;

            lock (_textureLock)
            {
                binary = GetBinary(gameTexture, id, shade, transform);
            }

            Span<T> span = MemoryMarshal.Cast<BGRA, T>(binary);
            return ref MemoryMarshal.GetReference(span);
        }

        private BGRA[] CalculateTexture(int id, int brightness)
        {
            if (gameTexture is BuildTexture buildTexture)
            {
                brightness = Math.Clamp(brightness, 0, 31);

                return TextureCache.GetTexture(buildTexture.Lookup, id, brightness);
            }
            else if (gameTexture is DoomTexture doomTexture)
            {
                BGRA[] texture = new BGRA[doomTexture.Texture.Length];
                doomTexture.Texture.AsSpan().CopyTo(texture);

                TextureTransformHelper.ShadeInPlace(texture, brightness);

                return texture;
            }

            throw new NotSupportedException();
        }

        private Dictionary<int, BGRA[]>[] GetOrAddTransformToShade(int id)
        {
            if (!gameTexture.PaletteToTransformToImage.TryGetValue(id, out Dictionary<int, BGRA[]>[]? transformToShade))
            {
                transformToShade = new Dictionary<int, BGRA[]>[1 + (int)TextureTransform.All];

                for (int i = 0; i < transformToShade.Length; i++)
                {
                    transformToShade[i] = [];
                }

                gameTexture.PaletteToTransformToImage[id] = transformToShade;
            }

            return transformToShade;
        }

        private BGRA[] GetOrAddNormal(int id, int shade)
        {
            Dictionary<int, BGRA[]>[] transformToShade = GetOrAddTransformToShade(gameTexture, id);

            var shadeToTexture = transformToShade[(int)TextureTransform.Normal];

            if (shadeToTexture.TryGetValue(shade, out BGRA[]? value))
            {
                return value;
            }

            value = CalculateTexture(gameTexture, id, shade);
            shadeToTexture[shade] = value;

            return value;
        }

        private Span<BGRA> GetBinary(int id, int shade, TextureTransform transform)
        {
            Dictionary<int, BGRA[]>[] transformToPallette = GetOrAddTransformToShade(gameTexture, id);

            Dictionary<int, BGRA[]> shadeToTexture = transformToPallette[(int)transform];

            if (shadeToTexture.TryGetValue(shade, out BGRA[]? texture))
            {
                return texture;
            }

            texture = GetOrAddNormal(gameTexture, id, shade);

            if (transform == TextureTransform.Normal)
            {
                shadeToTexture[shade] = texture;
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

            shadeToTexture[shade] = texture;
            return texture;
        }
    }

    extension(RenderColumnStatus status)
    {
        public bool IsFinished => status.HasFlag(RenderColumnStatus.FinishedRendering);
        public bool IsCalculated => status.HasFlag(RenderColumnStatus.Calculated);
        public bool CeilingRenderable => status.HasFlag(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderCeiling);
        public bool FloorRenderable => status.HasFlag(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderFloor);
        public bool WallRenderable => status.HasFlag(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderWall);
        public bool PortalRenderable => status.HasFlag(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderPortal);
    }
}
