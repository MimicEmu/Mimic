using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Mimic.Common.Memory
{
    public static class MemoryOperations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InPlaceAnd(Span<byte> input,
            in ReadOnlySpan<byte> with)
        {
            Debug.Assert(input.Length == with.Length);

            var longInput = input.NonPortableCast<byte, ulong>();
            var longWith = with.NonPortableCast<byte, ulong>();
            var longCount = input.Length / 8;

            for (int i = 0; i < longCount; i++)
                longInput[i] = longInput[i] & longWith[i];

            for (int i = longCount * 8; i < input.Length; i++)
                input[i] = (byte)(input[i] & with[i]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<byte> And(in ReadOnlySpan<byte> input,
            in ReadOnlySpan<byte> with)
        {
            Debug.Assert(input.Length == with.Length);

            Span<byte> result = new byte[input.Length];
            var longResult = result.NonPortableCast<byte, ulong>();

            var longInput = input.NonPortableCast<byte, ulong>();
            var longWith = with.NonPortableCast<byte, ulong>();
            var longCount = input.Length / 8;

            for (int i = 0; i < longCount; i++)
                longResult[i] = longInput[i] & longWith[i];

            for (int i = longCount * 8; i < input.Length; i++)
                result[i] = (byte)(input[i] & with[i]);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InPlaceOr(Span<byte> input,
            in ReadOnlySpan<byte> with)
        {
            Debug.Assert(input.Length == with.Length);

            var longInput = input.NonPortableCast<byte, ulong>();
            var longWith = with.NonPortableCast<byte, ulong>();
            var longCount = input.Length / 8;

            for (int i = 0; i < longCount; i++)
                longInput[i] = longInput[i] | longWith[i];

            for (int i = longCount * 8; i < input.Length; i++)
                input[i] = (byte)(input[i] | with[i]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<byte> Or(in ReadOnlySpan<byte> input,
            in ReadOnlySpan<byte> with)
        {
            Debug.Assert(input.Length == with.Length);

            Span<byte> result = new byte[input.Length];
            var longResult = result.NonPortableCast<byte, ulong>();

            var longInput = input.NonPortableCast<byte, ulong>();
            var longWith = with.NonPortableCast<byte, ulong>();
            var longCount = input.Length / 8;

            for (int i = 0; i < longCount; i++)
                longResult[i] = longInput[i] | longWith[i];

            for (int i = longCount * 8; i < input.Length; i++)
                result[i] = (byte)(input[i] | with[i]);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InPlaceXor(Span<byte> input,
            in ReadOnlySpan<byte> with)
        {
            Debug.Assert(input.Length == with.Length);

            var longInput = input.NonPortableCast<byte, ulong>();
            var longWith = with.NonPortableCast<byte, ulong>();
            var longCount = input.Length / 8;

            for (int i = 0; i < longCount; i++)
                longInput[i] = longInput[i] ^ longWith[i];

            for (int i = longCount * 8; i < input.Length; i++)
                input[i] = (byte)(input[i] ^ with[i]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<byte> Xor(in ReadOnlySpan<byte> input,
            in ReadOnlySpan<byte> with)
        {
            Debug.Assert(input.Length == with.Length);

            Span<byte> result = new byte[input.Length];
            var longResult = result.NonPortableCast<byte, ulong>();

            var longInput = input.NonPortableCast<byte, ulong>();
            var longWith = with.NonPortableCast<byte, ulong>();
            var longCount = input.Length / 8;

            for (int i = 0; i < longCount; i++)
                longResult[i] = longInput[i] ^ longWith[i];

            for (int i = longCount * 8; i < input.Length; i++)
                result[i] = (byte)(input[i] ^ with[i]);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InPlaceNot(Span<byte> input)
        {
            var longInput = input.NonPortableCast<byte, ulong>();
            var longCount = input.Length / 8;

            for (int i = 0; i < longCount; i++)
                longInput[i] = ~longInput[i];

            for (int i = longCount * 8; i < input.Length; i++)
                input[i] = (byte)~input[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<byte> Not(ReadOnlySpan<byte> input)
        {
            Span<byte> result = new byte[input.Length];
            var longResult = result.NonPortableCast<byte, ulong>();

            var longInput = input.NonPortableCast<byte, ulong>();
            var longCount = input.Length / 8;

            for (int i = 0; i < longCount; i++)
                longResult[i] = ~longInput[i];

            for (int i = longCount * 8; i < input.Length; i++)
                result[i] = (byte)~input[i];

            return result;
        }
    }
}
