using RenderingEngine.Models;
using RenderingEngine.Tooling;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private void RenderSkyboxVector(PortalPlayerSnapshot player, Sector sector)
        {
            const float oneOverTwoPi = 1f / (2 * MathF.PI);

            Span<int> wallStart = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped);
            Span<int> ceilingStart = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            Span<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);

            int width = PixelWidth;
            int height = PixelHeight;
            float viewAngle = player.Angle;

            TextureInfo textureInfo = sector.CeilTexture;
            Texture texture = TextureCache.GetTexture(textureInfo.Name);
            ref BGRA ceilingTexturePtr = ref MemoryMarshal.GetReference(texture.GetBinary(sector.CeilingShade, TextureTransform.Normal));
            ref BGRA screenPtr = ref GetScreenPtr<BGRA>();
            ref float angleCachePtr = ref memoryPool.GetBucketRef<float>(MemoryPoolBucket.AngleCache);

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            int textureWidth = texture.Width;
            int textureHeight = texture.Height;

            float textureWidth4 = textureWidth * 4f * oneOverTwoPi;

            int yTextureIncr = float.ConvertToInteger<int>((1f / height) * (textureHeight << 16));

            Vector<int> textureWidthV = Vector.Create(texture.Width);
            Vector<int> ivIncrF = new(yTextureIncr * Vector<float>.Count);

            Vector<int> incramentVector = Vector.CreateSequence(0, yTextureIncr);

            for (int x = sectorFromX; x <= sectorToX; x++)
            {
                RenderColumnStatus columnStatus = RenderWindowHelper.Status[x];

                if (!columnStatus.CeilingRenderable)
                {
                    continue;
                }

                // calculate angle between 0 to 2 PI
                float angleX = Unsafe.Add(ref angleCachePtr, x) - viewAngle;
                angleX = MathFormulas.ClampAngle(angleX);

                int texX = float.ConvertToIntegerNative<int>(textureWidth4 * angleX) % textureWidth;

                int wallStartY = wallStart[x];
                int ceilingStartY = ceilingStart[x];
                int floorEndY = floorEnd[x];
                int wallStartClampedY = Math.Clamp(wallStartY, ceilingStartY, floorEndY);

                int rem = (wallStartClampedY - ceilingStartY) % Vector<int>.Count;
                wallStartClampedY -= rem;

                ref BGRA screenColumnPtr = ref Unsafe.Add(ref screenPtr, x + width * ceilingStartY);
                ref BGRA screenEndColumnPtr = ref Unsafe.Add(ref screenPtr, x + width * wallStartClampedY);
                ref BGRA textureColumnPtr = ref Unsafe.Add(ref ceilingTexturePtr, texX);

                int vScreen = ceilingStartY * yTextureIncr;
                Vector<int> vScreenV = Vector.Create(vScreen) + incramentVector;

                for (; !Unsafe.AreSame(ref screenColumnPtr, ref screenEndColumnPtr); vScreenV += ivIncrF)
                {
                    Vector<int> texY = (vScreenV >> 16) * textureWidthV;

                    ref int texYPtr = ref Unsafe.As<Vector<int>, int>(ref texY);

                    for (int j = 0; j < Vector<int>.Count; j++)
                    {
                        int index = Unsafe.Add(ref texYPtr, j);

                        screenColumnPtr = Unsafe.Add(ref textureColumnPtr, index);
                        screenColumnPtr = ref Unsafe.Add(ref screenColumnPtr, width);
                    }
                }

                Vector<int> vScreenVInt = textureWidthV * (vScreenV >> 16);

                for (int y = 0; y < rem; y++)
                {
                    int index = vScreenVInt[y];
                    screenColumnPtr = Unsafe.Add(ref textureColumnPtr, index);
                    screenColumnPtr = ref Unsafe.Add(ref screenColumnPtr, width);
                }
            }
        }

        private void RenderSkyboxFloorVector(
            PortalPlayerSnapshot player,
            Sector sector)
        {
            const float oneOverTwoPi = 1f / (2 * MathF.PI);

            ReadOnlySpan<RenderColumnStatus> statusSpan = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            ReadOnlySpan<int> wallStartSpan = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped);
            ReadOnlySpan<int> ceilingStartSpan = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            ReadOnlySpan<int> floorEndSpan = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);

            int width = PixelWidth;
            int height = PixelHeight;
            float viewAngle = player.Angle;

            TextureInfo textureInfo = sector.FloorTexture;
            Texture texture = TextureCache.GetTexture(textureInfo.Name);
            ref BGRA ceilingTexturePtr = ref MemoryMarshal.GetReference(texture.GetBinary(sector.FloorShade, TextureTransform.Normal));
            ref BGRA screenPtr = ref GetScreenPtr<BGRA>();
            ref float angleCachePtr = ref memoryPool.GetBucketRef<float>(MemoryPoolBucket.AngleCache);

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            int textureWidth = texture.Width;
            int textureHeight = texture.Height;

            float textureWidth4 = textureWidth * 4f * oneOverTwoPi;
            float yTextureIncr = (1f / height) * textureHeight;

            Vector<int> textureWidthV = Vector.Create(texture.Width);

            Vector<float> ivIncrF = new(yTextureIncr * Vector<float>.Count);

            Vector<float> incramentVector = Vector.CreateSequence(0f, yTextureIncr);

            for (int x = sectorFromX; x <= sectorToX; x++)
            {
                RenderColumnStatus columnStatus = statusSpan[x];

                if (!columnStatus.FloorRenderable)
                {
                    continue;
                }

                // calculate angle between 0 to 2 PI
                float angleX = Unsafe.Add(ref angleCachePtr, x) - viewAngle;
                angleX = MathFormulas.ClampAngle(angleX);

                int texX = float.ConvertToIntegerNative<int>(textureWidth4 * angleX) % textureWidth;

                int wallStart = wallStartSpan[x];
                int ceilingStart = ceilingStartSpan[x];
                int floorEnd = floorEndSpan[x];
                int wallStartClamped = Math.Clamp(wallStart, ceilingStart, floorEnd);

                int rem = (floorEnd - wallStartClamped) % Vector<int>.Count;
                floorEnd -= rem;

                ref BGRA screenColumnPtr = ref Unsafe.Add(ref screenPtr, x + width * wallStartClamped);
                ref BGRA screenEndColumnPtr = ref Unsafe.Add(ref screenPtr, x + width * floorEnd);
                ref BGRA textureColumnPtr = ref Unsafe.Add(ref ceilingTexturePtr, texX);

                float vScreen = wallStartClamped * yTextureIncr;
                Vector<float> vScreenV = Vector.Create(vScreen) + incramentVector;

                for (; !Unsafe.AreSame(ref screenColumnPtr, ref screenEndColumnPtr); vScreenV += ivIncrF)
                {
                    Vector<int> texY = Vector.ConvertToInt32Native(vScreenV) * textureWidthV;

                    ref int texYPtr = ref Unsafe.As<Vector<int>, int>(ref texY);

                    for (int j = 0; j < Vector<int>.Count; j++)
                    {
                        int index = Unsafe.Add(ref texYPtr, j);

                        screenColumnPtr = Unsafe.Add(ref textureColumnPtr, index);
                        screenColumnPtr = ref Unsafe.Add(ref screenColumnPtr, width);
                    }
                }

                Vector<int> vScreenVInt = Vector.ConvertToInt32Native(vScreenV);

                for (int y = 0; y < rem; y++)
                {
                    int index = vScreenVInt[y] * textureWidth;

                    screenColumnPtr = Unsafe.Add(ref textureColumnPtr, index);
                    screenColumnPtr = ref Unsafe.Add(ref screenColumnPtr, width);
                }
            }

        }

    }
}
