namespace DoomAssetLoader.Texture
{
    public readonly struct PatchDescriptor
    {
        /// <summary>
        /// Offset from the top left corner of the texture space defined in fields 4 and 5 to start placement of this patch
        /// </summary>
        public readonly short XOffset;

        /// <summary>
        /// 
        /// </summary>
        public readonly short YOffset;

        /// <summary>
        /// The entry in the PNAMES lump that contains the lump name fromt the directory,
        /// of the wall patch to use
        /// </summary>
        public readonly short Number;

        /// <summary>
        /// Always 1
        /// </summary>
        public readonly short StepDir;

        /// <summary>
        /// Always 0
        /// </summary>
        public readonly short ColorMap;

        public PatchDescriptor(short xOffset, short yOffset, short number, short stepDir, short colorMap)
        {
            XOffset = xOffset;
            YOffset = yOffset;
            Number = number;
            StepDir = stepDir;
            ColorMap = colorMap;
        }
    }
}
