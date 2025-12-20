namespace DoomAssetLoader.Udmf
{
    public abstract class UdmfObject
    {
        public const string COMMENT = "comment";

        protected readonly Dictionary<string, string> properties = [];

        [DisallowNull]
        public string? this[string key] {
            get {
                _ = properties.TryGetValue(key, out string? value);
                return value;
            }
            set => properties[key] = value;
        }

        public void Add(string key, string value)
        {
            properties.Add(key, value);
        }

        public T? GetValue<T>(string key)
            where T : struct, IParsable<T>
        {
            if (properties.TryGetValue(key, out string? strValue))
            {
                return T.Parse(strValue, null);
            }

            return default;
        }

        public T GetRequiredValue<T>(string key)
            where T : struct, IParsable<T>
        {
            return T.Parse(properties[key], null);
        }
    }
}
