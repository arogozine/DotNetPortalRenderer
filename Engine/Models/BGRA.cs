namespace RenderingEngine.Models
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [SkipLocalsInit]
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct BGRA
    {
        public static readonly BGRA Blue = new(byte.MaxValue, 0, 0);

        public static readonly BGRA Green = new(0, byte.MaxValue, 0);

        public static readonly BGRA Red = new(0, 0, byte.MaxValue);

        public static readonly BGRA Yellow = new(0, byte.MaxValue, byte.MaxValue);

        public static readonly BGRA White = new(byte.MaxValue, byte.MaxValue, byte.MaxValue);

        public static readonly BGRA Black = new(0, 0, 0, byte.MaxValue);

        [FieldOffset(0)]
        public readonly uint Value;
        [FieldOffset(0)]
        public readonly byte B; // Blue
        [FieldOffset(1)]
        public readonly byte G; // Green
        [FieldOffset(2)]
        public readonly byte R; // Red
        [FieldOffset(3)]
        public readonly byte A; // Alpha (transparency)

        public readonly bool IsTransparent => A == 0;

        public BGRA(uint value)
        {
            Value = value;
        }

        public BGRA(byte blue, byte green, byte red, byte alpha = 255)
        {
            B = blue;
            G = green;
            R = red;
            A = alpha;
        }

        public override readonly string ToString()
        {
            return $"BGRA({B}, {G}, {R}, {A})";
        }

        public override bool Equals(object? obj)
        {
            if (obj is BGRA other)
            {
                return Equals(other);
            }

            return false;
        }

        public bool Equals(BGRA other)
        {
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (int)Value;
            }
        }

        public static bool operator ==(BGRA left, BGRA right)
        {
            return left.Value == right.Value;
        }

        public static bool operator !=(BGRA left, BGRA right)
        {
            return left.Value != right.Value;
        }

        public static implicit operator BGRA(int value)
        {
            unchecked
            {
                return new BGRA((uint)value);
            }
        }

        public static implicit operator BGRA(uint value)
        {
            return new BGRA(value);
        }
    }

}
