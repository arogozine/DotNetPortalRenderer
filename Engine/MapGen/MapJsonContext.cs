using RenderingEngine.Models.Json;
using System.Text.Json.Serialization;

namespace RenderingEngine.MapGen
{
    [JsonSerializable(typeof(Map))]
    internal partial class MapJsonContext : JsonSerializerContext
    {

    }
}
