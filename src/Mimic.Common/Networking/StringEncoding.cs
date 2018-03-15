namespace Mimic.Common.Networking
{
    public enum StringEncoding
    {
        LengthPrefixedInt8,
        LengthPrefixedUInt8,
        LengthPrefixedInt16,
        LengthPrefixedUInt16,
        LengthPrefixedInt32,
        LengthPrefixedUInt32,
        LengthPrefixedInt64,
        LengthPrefixedUInt64,
        NullTerminated,
        FixedLength
    }
}
