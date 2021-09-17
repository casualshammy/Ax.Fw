using System;

namespace Ax.Fw
{
    public class SerializableVersion : IEquatable<SerializableVersion>
    {
        public SerializableVersion(Version version)
        {
            Major = version.Major;
            Minor = version.Minor;
            Build = version.Build;
        }

        public SerializableVersion(int major, int minor, int build)
        {
            Major = major;
            Minor = minor;
            Build = build;
        }

        public SerializableVersion()
        {
            Major = 0;
            Minor = 0;
            Build = 0;
        }

        public int Major { get; set; }

        public int Minor { get; set; }

        public int Build { get; set; }

        public static bool operator ==(SerializableVersion a, SerializableVersion b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a is null || b is null)
                return false;
            return a.Major == b.Major && a.Minor == b.Minor && a.Build == b.Build;
        }

        public static bool operator !=(SerializableVersion a, SerializableVersion b)
        {
            return !(a == b);
        }

        public static bool operator >(SerializableVersion a, SerializableVersion b)
        {
            if (a.Major < b.Major)
                return false;
            if (a.Major > b.Major)
                return true;
            if (a.Minor < b.Minor)
                return false;
            if (a.Minor > b.Minor)
                return true;
            if (a.Build < b.Build)
                return false;
            if (a.Build > b.Build)
                return true;
            return false;
        }

        public static bool operator <(SerializableVersion a, SerializableVersion b)
        {
            if (a.Major < b.Major)
                return true;
            if (a.Major > b.Major)
                return false;
            if (a.Minor < b.Minor)
                return true;
            if (a.Minor > b.Minor)
                return false;
            if (a.Build < b.Build)
                return true;
            if (a.Build > b.Build)
                return false;
            return false;
        }

        public static bool operator <=(SerializableVersion a, SerializableVersion b)
        {
            return a < b || a == b;
        }

        public static bool operator >=(SerializableVersion a, SerializableVersion b)
        {
            return a > b || a == b;
        }

        public bool Equals(SerializableVersion other)
        {
            return Major == other.Major && Minor == other.Minor && Build == other.Build;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((SerializableVersion)obj);
        }

        public override int GetHashCode()
        {
            return Major ^ Minor ^ Build;
        }

        public override string ToString()
        {
            return Major + "." + Minor + "." + Build;
        }
    }
}
