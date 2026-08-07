namespace BuildAssetLoader.Con
{
    public sealed record OneventCommand(string EventName) : BaseEventCommand(CommandList.onevent, EventName);
}

