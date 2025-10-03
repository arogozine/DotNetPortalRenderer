namespace RenderingEngine.DoomMapLoader.Udmf
{
    internal sealed class UdmfMapData
    {
        public List<UdmfVertex> Vertices { get; } = [];
        public List<UdmfLinedef> Linedefs { get; } = [];
        public List<UdmfSidedef> Sidedefs { get; } = [];
        public List<UdmfSector> Sectors { get; } = [];
        public List<UdmfThing> Things { get; } = [];
    }
}
