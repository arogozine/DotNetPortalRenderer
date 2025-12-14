namespace BuildAssetLoader
{
    public class GrpFile
    {
        public Dictionary<string, byte[]> Files { get; }

        public GrpFile(Dictionary<string, byte[]> files)
        {
            Files = files;
        }
    }
}
