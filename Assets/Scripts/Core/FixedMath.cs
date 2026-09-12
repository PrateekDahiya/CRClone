using System;
using UnityEngine;

namespace CRClone.Core.Math
{
    /// <summary>
    /// Fixed-point math with 1/1024 precision for deterministic cross-platform calculations.
    /// All simulation math should use this instead of float/double.
    /// </summary>
    public readonly struct Fixed : IEquatable<Fixed>, IComparable<Fixed>
    {
        public const int FRACTIONAL_BITS = 10;
        public const int SCALE = 1 << FRACTIONAL_BITS; // 1024
        public const int HALF_SCALE = SCALE / 2;
        
        private readonly int _value;

        public Fixed(int rawValue)
        {
            _value = rawValue;
        }

        public static Fixed FromFloat(float f) => new Fixed(Mathf.RoundToInt(f * SCALE));
        public static Fixed FromInt(int i) => new Fixed(i << FRACTIONAL_BITS);
        
        public float ToFloat() => _value / (float)SCALE;
        public int ToInt() => _value >> FRACTIONAL_BITS;
        public int RawValue => _value;

        // Constants
        public static Fixed Zero => new Fixed(0);
        public static Fixed One => new Fixed(SCALE);
        public static Fixed Half => new Fixed(HALF_SCALE);
        public static Fixed MinValue => new Fixed(int.MinValue);
        public static Fixed MaxValue => new Fixed(int.MaxValue);

        // Operators
        public static Fixed operator +(Fixed a, Fixed b) => new Fixed(a._value + b._value);
        public static Fixed operator -(Fixed a, Fixed b) => new Fixed(a._value - b._value);
        public static Fixed operator -(Fixed a) => new Fixed(-a._value);
        
        public static Fixed operator *(Fixed a, Fixed b) 
            => new Fixed((int)(((long)a._value * b._value) >> FRACTIONAL_BITS));
        
        public static Fixed operator *(Fixed a, int b) => new Fixed(a._value * b);
        public static Fixed operator *(int a, Fixed b) => new Fixed(a * b._value);
        
        public static Fixed operator /(Fixed a, Fixed b) 
            => new Fixed((int)(((long)a._value << FRACTIONAL_BITS) / b._value));
        
        public static Fixed operator /(Fixed a, int b) => new Fixed(a._value / b);

        // Comparison
        public static bool operator ==(Fixed a, Fixed b) => a._value == b._value;
        public static bool operator !=(Fixed a, Fixed b) => a._value != b._value;
        public static bool operator <(Fixed a, Fixed b) => a._value < b._value;
        public static bool operator >(Fixed a, Fixed b) => a._value > b._value;
        public static bool operator <=(Fixed a, Fixed b) => a._value <= b._value;
        public static bool operator >=(Fixed a, Fixed b) => a._value >= b._value;

        public bool Equals(Fixed other) => _value == other._value;
        public override bool Equals(object obj) => obj is Fixed other && Equals(other);
        public override int GetHashCode() => _value;
        public int CompareTo(Fixed other) => _value.CompareTo(other._value);

        public override string ToString() => ToFloat().ToString("F4");

        // Math functions
        public static Fixed Abs(Fixed a) => new Fixed(Math.Abs(a._value));
        public static Fixed Min(Fixed a, Fixed b) => a < b ? a : b;
        public static Fixed Max(Fixed a, Fixed b) => a > b ? a : b;
        public static Fixed Clamp(Fixed value, Fixed min, Fixed max) => Max(min, Min(max, value));
        
        public static Fixed Sqrt(Fixed a)
        {
            if (a._value <= 0) return Zero;
            // Newton's method for fixed-point sqrt
            long x = a._value << FRACTIONAL_BITS; // Scale up for precision
            long root = (long)Math.Sqrt(x);
            return new Fixed((int)root);
        }

        public static Fixed Lerp(Fixed a, Fixed b, Fixed t) => a + (b - a) * t;
        
        // Trigonometry (using lookup tables for determinism)
        private static readonly Fixed[] SinTable = GenerateSinTable();
        private static readonly Fixed[] CosTable = GenerateCosTable();
        private const int TRIG_TABLE_SIZE = 1024;

        private static Fixed[] GenerateSinTable()
        {
            var table = new Fixed[TRIG_TABLE_SIZE];
            for (int i = 0; i < TRIG_TABLE_SIZE; i++)
            {
                float rad = (i * 2f * Mathf.PI) / TRIG_TABLE_SIZE;
                table[i] = FromFloat(Mathf.Sin(rad));
            }
            return table;
        }

        private static Fixed[] GenerateCosTable()
        {
            var table = new Fixed[TRIG_TABLE_SIZE];
            for (int i = 0; i < TRIG_TABLE_SIZE; i++)
            {
                float rad = (i * 2f * Mathf.PI) / TRIG_TABLE_SIZE;
                table[i] = FromFloat(Mathf.Cos(rad));
            }
            return table;
        }

        public static Fixed Sin(Fixed radians)
        {
            int index = (int)(((long)radians._value * TRIG_TABLE_SIZE) / (long)(2 * Mathf.PI * SCALE)) & (TRIG_TABLE_SIZE - 1);
            return SinTable[index];
        }

        public static Fixed Cos(Fixed radians)
        {
            int index = (int)(((long)radians._value * TRIG_TABLE_SIZE) / (long)(2 * Mathf.PI * SCALE)) & (TRIG_TABLE_SIZE - 1);
            return CosTable[index];
        }

        // Conversion from Vector2
        public static FixedVector2 ToFixedVector2(Vector2 v) => new FixedVector2(FromFloat(v.x), FromFloat(v.y));
    }

    /// <summary>
    /// 2D vector using Fixed-point math for deterministic simulation.
    /// </summary>
    public readonly struct FixedVector2 : IEquatable<FixedVector2>
    {
        public readonly Fixed x;
        public readonly Fixed y;

        public FixedVector2(Fixed x, Fixed y)
        {
            this.x = x;
            this.y = y;
        }

        public FixedVector2(float x, float y) : this(Fixed.FromFloat(x), Fixed.FromFloat(y)) { }
        public FixedVector2(int x, int y) : this(new Fixed(x), new Fixed(y)) { }

        public static FixedVector2 Zero => new FixedVector2(Fixed.Zero, Fixed.Zero);
        public static FixedVector2 One => new FixedVector2(Fixed.One, Fixed.One);
        public static FixedVector2 Up => new FixedVector2(Fixed.Zero, Fixed.One);
        public static FixedVector2 Down => new FixedVector2(Fixed.Zero, -Fixed.One);
        public static FixedVector2 Left => new FixedVector2(-Fixed.One, Fixed.Zero);
        public static FixedVector2 Right => new FixedVector2(Fixed.One, Fixed.Zero);

        // Operators
        public static FixedVector2 operator +(FixedVector2 a, FixedVector2 b) => new FixedVector2(a.x + b.x, a.y + b.y);
        public static FixedVector2 operator -(FixedVector2 a, FixedVector2 b) => new FixedVector2(a.x - b.x, a.y - b.y);
        public static FixedVector2 operator -(FixedVector2 a) => new FixedVector2(-a.x, -a.y);
        public static FixedVector2 operator *(FixedVector2 a, Fixed b) => new FixedVector2(a.x * b, a.y * b);
        public static FixedVector2 operator *(Fixed a, FixedVector2 b) => new FixedVector2(a * b.x, a * b.y);
        public static FixedVector2 operator /(FixedVector2 a, Fixed b) => new FixedVector2(a.x / b, a.y / b);

        public static bool operator ==(FixedVector2 a, FixedVector2 b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(FixedVector2 a, FixedVector2 b) => a.x != b.x || a.y != b.y;

        public bool Equals(FixedVector2 other) => x == other.x && y == other.y;
        public override bool Equals(object obj) => obj is FixedVector2 other && Equals(other);
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2);

        // Vector operations
        public Fixed SqrMagnitude => x * x + y * y;
        public Fixed Magnitude => Fixed.Sqrt(SqrMagnitude);
        
        public Fixed DistanceTo(FixedVector2 other) => (other - this).Magnitude;
        
        public FixedVector2 Normalized
        {
            get
            {
                Fixed mag = Magnitude;
                return mag > Fixed.Zero ? this / mag : Zero;
            }
        }

        public FixedVector2 ClampMagnitude(Fixed maxLength)
        {
            Fixed sqrMag = SqrMagnitude;
            if (sqrMag > maxLength * maxLength)
            {
                return Normalized * maxLength;
            }
            return this;
        }

        public static Fixed Dot(FixedVector2 a, FixedVector2 b) => a.x * b.x + a.y * b.y;
        public static Fixed Cross(FixedVector2 a, FixedVector2 b) => a.x * b.y - a.y * b.x;

        public static FixedVector2 Lerp(FixedVector2 a, FixedVector2 b, Fixed t) => a + (b - a) * t;
        public static FixedVector2 MoveTowards(FixedVector2 current, FixedVector2 target, Fixed maxDistanceDelta)
        {
            FixedVector2 toTarget = target - current;
            Fixed dist = toTarget.Magnitude;
            if (dist <= maxDistanceDelta || dist == Fixed.Zero)
                return target;
            return current + toTarget / dist * maxDistanceDelta;
        }

        // Conversion
        public Vector2 ToVector2() => new Vector2(x.ToFloat(), y.ToFloat());
        public UnityEngine.Vector2 ToUnityVector2() => new UnityEngine.Vector2(x.ToFloat(), y.ToFloat());
        
        public static implicit operator Vector2(FixedVector2 v) => v.ToVector2();
        public static implicit operator FixedVector2(Vector2 v) => new FixedVector2(Fixed.FromFloat(v.x), Fixed.FromFloat(v.y));
        public static implicit operator FixedVector2(UnityEngine.Vector2 v) => new FixedVector2(Fixed.FromFloat(v.x), Fixed.FromFloat(v.y));

        public override string ToString() => $"({x}, {y})";
    }

    /// <summary>
    /// Deterministic random number generator using Xorshift64*.
    /// Same seed always produces identical sequence across platforms.
    /// </summary>
    public struct DeterministicRNG
    {
        private ulong _state;

        public DeterministicRNG(ulong seed)
        {
            _state = seed != 0 ? seed : 0x9E3779B97F4A7C15; // Default seed if 0
        }

        public uint NextUInt()
        {
            _state ^= _state >> 12;
            _state ^= _state << 25;
            _state ^= _state >> 27;
            return (uint)(_state * 0x2545F4914F6CDD1D);
        }

        public ulong NextULong()
        {
            _state ^= _state >> 12;
            _state ^= _state << 25;
            _state ^= _state >> 27;
            return _state * 0x2545F4914F6CDD1D;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive) return minInclusive;
            uint range = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % range);
        }

        public float NextFloat() => NextUInt() * (1f / 0xFFFFFFFFu);
        
        public double NextDouble() => NextUInt() * (1.0 / 0xFFFFFFFFu);
        
        public Fixed NextFixed() => Fixed.FromFloat(NextFloat());
        public Fixed NextFixed(Fixed min, Fixed max) => min + (max - min) * NextFixed();

        public bool NextBool(float probability) => NextFloat() < probability;
        
        public FixedVector2 NextVector2(Fixed minX, Fixed maxX, Fixed minY, Fixed maxY)
            => new FixedVector2(NextFixed(minX, maxX), NextFixed(minY, maxY));

        public FixedVector2 NextVector2InCircle(Fixed radius)
        {
            // Uniform distribution in circle
            Fixed r = Fixed.Sqrt(NextFixed()) * radius;
            Fixed angle = NextFixed() * Fixed.FromFloat(2f * Mathf.PI);
            return new FixedVector2(Fixed.Cos(angle) * r, Fixed.Sin(angle) * r);
        }

        public FixedVector2 NextVector2InRing(Fixed minRadius, Fixed maxRadius)
        {
            Fixed r = Fixed.Sqrt(NextFixed(minRadius * minRadius, maxRadius * maxRadius));
            Fixed angle = NextFixed() * Fixed.FromFloat(2f * Mathf.PI);
            return new FixedVector2(Fixed.Cos(angle) * r, Fixed.Sin(angle) * r);
        }

        public void SetState(ulong state) => _state = state;
        public ulong GetState() => _state;
    }
}