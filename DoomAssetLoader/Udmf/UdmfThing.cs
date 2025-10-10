using System.ComponentModel;

namespace DoomAssetLoader.Udmf
{
    public class UdmfThing : UdmfObject
    {
        public const string ID = "id";
        public const string POSITION_X = "x";
        public const string POSITION_Y = "y";
        public const string ANGLE = "angle";
        public const string TYPE = "type";
        public const string FLAG_SKILL1 = "skill1";
        public const string FLAG_SKILL2 = "skill2";
        public const string FLAG_SKILL3 = "skill3";
        public const string FLAG_SKILL4 = "skill4";
        public const string FLAG_SKILL5 = "skill5";
        public const string FLAG_AMBUSH = "ambush";
        public const string FLAG_SINGLE_PLAYER = "single";
        public const string FLAG_COOPERATIVE = "coop";
        public const string FLAG_DEATHMATCH = "dm";

        /// <summary>
        ///  Thing ID. Default = 0.
        /// </summary>
        [DefaultValue(0)]
        public int Id => GetValue<int>(ANGLE) ?? 0;

        /// <summary>
        /// X coordinate.
        /// </summary>
        public float X => GetRequiredValue<float>(POSITION_X);

        /// <summary>
        /// Y coordinate
        /// </summary>
        public float Y => GetRequiredValue<float>(POSITION_Y);

        /// <summary>
        /// DoomedNum
        /// </summary>
        public int Type => GetRequiredValue<int>(TYPE);

        /// <summary>
        /// Map angle of thing in degrees. Default = 0 (East).
        /// </summary>
        [DefaultValue(0)]
        public int Angle => GetValue<int>(ANGLE) ?? 0;
    }
}
