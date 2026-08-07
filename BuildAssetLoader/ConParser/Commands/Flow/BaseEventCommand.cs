namespace BuildAssetLoader.Con
{
    // ===== Global Settings - Procedural (events) =====

    // onevent <EVENT_NAME> ... endevent / appendevent <EVENT_NAME> ... endevent
    public record BaseEventCommand(CommandList Start, string EventName) : Structure(Start, CommandList.endevent);
}

