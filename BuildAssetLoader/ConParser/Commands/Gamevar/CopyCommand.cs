namespace BuildAssetLoader.Con
{
    // copy <src_array>[<src_index>] <dst_array>[<dst_index>] <size>
    public sealed record CopyCommand(string SrcArray, string SrcIndex, string DstArray, string DstIndex, string Size)
        : Command(CommandList.copy);
}

