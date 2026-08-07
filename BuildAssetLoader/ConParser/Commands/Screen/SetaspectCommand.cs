namespace BuildAssetLoader.Con
{
    // setaspect <viewingrange> <yxaspect> — sets renderer field of view; only valid in screen drawing events.
    public sealed record SetaspectCommand(string ViewingRange, string YxAspect) : Command(CommandList.setaspect);
}

