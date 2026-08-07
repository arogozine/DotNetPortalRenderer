namespace BuildAssetLoader.Con
{
    public sealed record AppendeventCommand(string EventName) : BaseEventCommand(CommandList.appendevent, EventName);
}

