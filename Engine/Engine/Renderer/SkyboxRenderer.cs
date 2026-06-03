using RenderingEngine.Tooling;
using SoftwareRendererModels;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private unsafe void RenderSkyboxVector(PortalPlayerSnapshot player, RenderableSector sector)
        {
            GameTextureInfo textureInfo = sector.CeilTexture;
            GameTexture texture = TextureCache.GetTexture(textureInfo.Name);

            ref uint ceilingTexturePtr = ref texture.GetBinaryRef<uint>(sector.CeilingShade, TextureTransform.Normal);

            uint* screenPtr = (uint*)buffer;

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            int* wallStartPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.WallStartClamped);
            int* ceilingStartPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.CeilingStart);
            int* floorEndPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.FloorEnd);

            fixed (uint* texturePtr = &ceilingTexturePtr)
            {
                Sse.Prefetch2(texturePtr);

                RenderSkyboxShared(player, RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderCeiling,
                    screenPtr, texturePtr, sectorFromX, sectorToX,
                    ceilingStartPtr, wallStartPtr,
                    ceilingStartPtr, floorEndPtr,
                    PixelWidth,
                    texture.Width, texture.Height);
            }
        }

        private unsafe void RenderSkyboxShared(PortalPlayerSnapshot player,
            RenderColumnStatus renderColumnStatus,
            uint* screenPtr,
            uint* texturePtr,
            int sectorFromX, int sectorToX,
            int* fromYPtr, int* toYPtr,
            int* ceilingStartPtr, int* floorEndPtr,
            int width,
            int textureWidth,
            int textureHeight)
        {
            float* angleCache = stackalloc float[Vector<float>.Count];

            const float oneOverTwoPi = 1f / (2 * MathF.PI);
            float viewAngle = player.Angle;
            float textureWidth4 = textureWidth * 4f * oneOverTwoPi;

            float yTextureIncr = ((float)textureHeight) / PixelHeight;

            Vector<float> textureWidth4V = Vector.Create(textureWidth4);
            Vector<float> yTextureIncrV = Vector.Create(yTextureIncr);
            Vector<float> viewAngleV = Vector.Create(viewAngle);
            Vector<int> widthMask = Vector.Create(textureWidth - 1);

            float* angleCachePtr = memoryPool.GetBucketPtr<float>(MemoryPoolBucket.AngleCache);

            Span<ushort> repeatedCount = CaclulateRepeatedCount();
            _ = SharedHelpers.PopulateRepeatedValuesInPlace(repeatedCount);

            for (int x = sectorFromX; x < sectorToX;)
            {
                ushort count = repeatedCount[x - sectorFromX];

                if (count == 0)
                {
                    x++;
                    continue;
                }

                if (count >= Vector<int>.Count)
                {
                    Vector<int> wallStartY = Vector.Load(fromYPtr + x);
                    Vector<int> wallEndY = Vector.Load(toYPtr + x);

                    Vector<int> ceilingStartY = Vector.Load(ceilingStartPtr + x);
                    Vector<int> floorEndY = Vector.Load(floorEndPtr + x);

                    wallStartY = Vector.ClampNative(wallStartY, ceilingStartY, floorEndY);
                    wallEndY = Vector.ClampNative(wallEndY, ceilingStartY, floorEndY);

                    (int min_t, int max_t, int min_b, int max_b) = CalculateLaneTopBottoms(wallStartY, wallEndY);

                    RenderLine(x, wallEndY, wallStartY, min_t, max_t, min_b, max_b);

                    x += Vector<int>.Count;
                    count -= (ushort)Vector<int>.Count;

                    continue;
                }

                while (count-- > 0)
                {
                    int wallStartY = fromYPtr[x];
                    int wallEndY = toYPtr[x];

                    int ceilingStartY = ceilingStartPtr[x];
                    int floorEndY = floorEndPtr[x];

                    wallStartY = Math.Clamp(wallStartY, ceilingStartY, floorEndY);
                    wallEndY = Math.Clamp(wallEndY, ceilingStartY, floorEndY);


                    RenderColumn(player, wallStartY, wallEndY, x);

                    x++;
                }
            }

            return;


            static (int min_t, int max_t, int min_b, int max_b) CalculateLaneTopBottoms(Vector<int> from, Vector<int> to)
            {
                if (Vector<int>.Count == 8)
                {
                    Vector256<int> fromV = from.AsVector256();
                    Vector256<int> toV = to.AsVector256();

                    (int min_t, int max_t) = MathFormulas.GetMinMaxValue(fromV);
                    (int min_b, int max_b) = MathFormulas.GetMinMaxValue(toV);

                    return (min_t, max_t, min_b, max_b);
                }
                else if (Vector<int>.Count == 4)
                {
                    Vector128<int> fromV = from.AsVector128();
                    Vector128<int> toV = to.AsVector128();

                    (int min_t, int max_t) = MathFormulas.GetMinMaxValue(fromV);
                    (int min_b, int max_b) = MathFormulas.GetMinMaxValue(toV);

                    return (min_t, max_t, min_b, max_b);
                }
                else
                {
                    int min_t = int.MaxValue, max_t = int.MinValue;
                    int min_b = int.MaxValue, max_b = int.MinValue;

                    // compute per-lane tops/bottoms
                    for (int i = 0; i < Vector<int>.Count; i++)
                    {
                        int top = from[i];
                        min_t = MathFormulas.Min(min_t, top);
                        max_t = MathFormulas.Max(max_t, top);

                        int bottom = to[i];
                        min_b = MathFormulas.Min(min_b, bottom);
                        max_b = MathFormulas.Max(max_b, bottom);
                    }

                    return (min_t, max_t, min_b, max_b);
                }
            }


            void RenderLine(
                int x,
                Vector<int> to, Vector<int> from,
                int min_t, int max_t, int min_b, int max_b
                )
            {
                Vector<float> angleXV = Vector.Load(angleCachePtr + x) - viewAngleV;
                for (int i = 0; i < Vector<float>.Count; i++)
                {
                    angleCache[i] = MathFormulas.ClampAngle(angleXV[i]);
                }
                angleXV = Vector.Load(angleCache);

                Vector<float> vScreenV = max_t * yTextureIncrV;
                Vector<int> texXV = Vector.ConvertToInt32Native(textureWidth4V * angleXV) & widthMask;

                // render tops where there is no shared window
                if (min_t != max_t)
                {
                    RenderColumnAngleTop(min_t, max_t, from, texXV, x);
                }

                uint* fromPtr = screenPtr + max_t * width + x;

                if (Avx2.IsSupported && Vector<uint>.Count == Vector256<uint>.Count)
                {
                    for (int y = max_t; y <= min_b; y++)
                    {
                        Vector<int> textureIndex = texXV + textureWidth * Vector.ConvertToInt32Native(vScreenV);
                        Vector256<uint> gathered = Avx2.GatherVector256(texturePtr, textureIndex.AsVector256(), scale: sizeof(int));
                        gathered.Store(fromPtr);

                        fromPtr += width;
                        vScreenV += yTextureIncrV;
                    }
                }
                else
                {
                    for (int y = max_t; y <= min_b; y++)
                    {
                        Vector<int> textureIndex = texXV + textureWidth * Vector.ConvertToInt32Native(vScreenV);

                        for (int i = 0; i < Vector<float>.Count; i++)
                        {
                            *(fromPtr + i) = *(texturePtr + textureIndex[i]);
                        }

                        fromPtr += width;
                        vScreenV += yTextureIncrV;
                    }
                }

                // render bottoms where there is no shared window
                if (min_b != max_b)
                {
                    RenderColumnAngleBottom(min_b, max_b, to, texXV, x);
                }
            }

            void RenderColumnAngleBottom(
                int min_b,
                int max_b,
                Vector<int> to,
                Vector<int> texXV,
                int xStart)
            {
                uint* screenTexPtr = screenPtr + min_b * width + xStart;

                float vScreen = min_b * yTextureIncr;

                for (int y = min_b; y < max_b; y++)
                {
                    for (int i = 0; i < Vector<uint>.Count; i++)
                    {
                        if (to[i] <= y)
                        {
                            continue;
                        }

                        int index = texXV[i] + textureWidth * float.ConvertToIntegerNative<int>(vScreen);
                        screenTexPtr[i] = texturePtr[index];

                    }

                    screenTexPtr += width;
                    vScreen += yTextureIncr;
                }
            }

            void RenderColumnAngleTop(
                int min_t,
                int max_t,
                Vector<int> from,
                Vector<int> texXV,
                int xStart)
            {
                uint* screenTexPtr = screenPtr + min_t * width + xStart;

                float vScreen = min_t * yTextureIncr;

                for (int y = min_t; y < max_t; y++)
                {
                    for (int i = 0; i < Vector<uint>.Count; i++)
                    {
                        if (from[i] >= y)
                        {
                            continue;
                        }

                        int index = texXV[i] + textureWidth * float.ConvertToIntegerNative<int>(vScreen);
                        screenTexPtr[i] = texturePtr[index];
                    }

                    screenTexPtr += width;
                    vScreen += yTextureIncr;
                }
            }

            void RenderColumn(PortalPlayerSnapshot player, int fromY, int toY, int x)
            {
                uint* fromPtr = screenPtr + fromY * width + x;
                uint* toPtr = screenPtr + toY * width + x;

                float angleX = *(angleCachePtr + x) - viewAngle;
                angleX = MathFormulas.ClampAngle(angleX);

                int texX = float.ConvertToIntegerNative<int>(textureWidth4 * angleX) % textureWidth;

                float vScreen = fromY * yTextureIncr;
                uint* textureColumnPtr = texturePtr + texX;

                for (; fromPtr < toPtr; vScreen += yTextureIncr, fromPtr += width)
                {
                    int index = textureWidth * float.ConvertToIntegerNative<int>(vScreen);
                    uint tex = *(textureColumnPtr + index);
                    *fromPtr = tex;
                }
            }

            Span<ushort> CaclulateRepeatedCount()
            {
                RenderColumnStatus* status = memoryPool.GetBucketPtr<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);

                int length = sectorToX - sectorFromX;

                Span<ushort> repeatedCount = memoryPool.GetBucket<ushort>(MemoryPoolBucket.Temp2)[..(length + 1)];

                for (int x = sectorFromX; x <= sectorToX; x++)
                {
                    RenderColumnStatus columnStatus = status[x];

                    if (!columnStatus.HasFlag(renderColumnStatus))
                    {
                        repeatedCount[x - sectorFromX] = 0;
                        continue;
                    }

                    repeatedCount[x - sectorFromX] = (ushort)length;
                }

                return repeatedCount;
            }
        }

        private unsafe void RenderSkyboxFloorVector(
            PortalPlayerSnapshot player,
            RenderableSector sector)
        {
            GameTextureInfo textureInfo = sector.FloorTexture;
            GameTexture texture = TextureCache.GetTexture(textureInfo.Name);

            ref uint ceilingTexturePtr = ref texture.GetBinaryRef<uint>(sector.CeilingShade, TextureTransform.Normal);

            uint* screenPtr = (uint*)buffer;

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            int* wallEndPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.WallEndClamped);
            int* ceilingStartPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.CeilingStart);
            int* floorEndPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.FloorEnd);

            fixed (uint* texturePtr = &ceilingTexturePtr)
            {
                Sse.Prefetch2(texturePtr);

                RenderSkyboxShared(player, RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderFloor,
                    screenPtr, texturePtr, sectorFromX, sectorToX,
                    wallEndPtr, floorEndPtr,
                    ceilingStartPtr, floorEndPtr,
                    PixelWidth,
                    texture.Width, texture.Height);
            }
        }

        private unsafe bool DrawBasicSkyboxWall(
            PortalPlayerSnapshot player,
            RenderablePortalWall renderableWall)
        {
            int* wallStartPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.WallStartClamped);
            int* wallEndPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.WallEndClamped);

            RenderableWall wall = renderableWall.Wall;
            Debug.Assert(wall.MiddleTexture != null);
            DrawBasicSkyboxWall(player, renderableWall, wallStartPtr, wallEndPtr, wall.MiddleTexture);

            // Set render status to finished
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            Span<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            status[wallFromX..wallToX].Fill(RenderColumnStatus.FinishedRendering);

            return true;
        }

        private unsafe void DrawBasicSkyboxWall(
            PortalPlayerSnapshot player,
            RenderablePortalWall renderableWall,
            int* wallStartPtr, int* wallEndPtr,
            GameTextureInfo wallTexture)
        {
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            GameTextureInfo textureInfo = wallTexture;
            ref uint wallTextureUintPtr = ref wallTexture.Texture.GetBinaryRef<uint>(renderableWall.Wall.Shade ?? byte.MaxValue, TextureTransform.Normal);

            uint* screenPtr = (uint*)buffer;

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            int* ceilingStartPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.CeilingStart);
            int* floorEndPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.FloorEnd);

            fixed (uint* texturePtr = &wallTextureUintPtr)
            {
                Sse.Prefetch2(texturePtr);

                RenderSkyboxShared(player, RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderWall,
                    screenPtr, texturePtr, wallFromX, wallToX,
                    wallStartPtr, wallEndPtr,
                    ceilingStartPtr, floorEndPtr,
                    PixelWidth,
                    wallTexture.Width, wallTexture.Height);
            }
        }
    }
}
