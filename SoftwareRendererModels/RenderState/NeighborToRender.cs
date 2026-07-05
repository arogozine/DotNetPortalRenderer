using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SoftwareRendererModels;

[DebuggerDisplay("SectorId = {SectorId}")]
public sealed class NeighborsToRender : IRenderState, IDisposable
{
    private int _length;
    private RenderableWall[]? _parentWalls;

    public RenderableWall? MirrorWall { get; set; }
    public int SectorId { get; set; }
    public RenderablePortalWall? RenderableWall { get; private set; }
    public ReadOnlySpan<RenderableWall> ParentWalls => _parentWalls.AsSpan()[.._length];

    [MemberNotNull(nameof(_parentWalls))]
    public void Initialize(int sectorId)
    {
        _length = 0;

        SectorId = sectorId;
        RenderableWall = null;
        MirrorWall = null;
        _parentWalls = [];
    }

    public (RenderableWall[]? ParentWalls, int Lenth) GetParentWallsArray() => (_parentWalls, _length);

    [MemberNotNull(nameof(_parentWalls))]
    public void Initialize(RenderablePortalWall renderableWall, scoped ReadOnlySpan<RenderableWall> walls, int sectorId)
    {
        _length = walls.Length + 1;

        _parentWalls = ArrayPool<RenderableWall>.Shared.Rent(_length);
        walls.CopyTo(_parentWalls);
        _parentWalls[walls.Length] = renderableWall.Wall;

        SectorId = sectorId;
        RenderableWall = renderableWall;
        MirrorWall = null;
    }

    public void Reset()
    {
        if (_parentWalls is { })
        {
            ArrayPool<RenderableWall>.Shared.Return(_parentWalls);
        }

        _length = 0;

        SectorId = 0;
        RenderableWall = null;
        MirrorWall = null;
        _parentWalls = [];
    }

    public void Dispose()
    {
        if (_parentWalls is { })
        {
            ArrayPool<RenderableWall>.Shared.Return(_parentWalls);
        }

        GC.SuppressFinalize(this);
    }

}
