using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Mimic.Common.Networking
{
    public class AsyncBinaryWriter : IDisposable
    {
        private readonly Stream _underlyingStream;
        private readonly Encoding _textEncoding;
        private readonly bool _closeOnDispose;

        private readonly MemoryStream _buffer;

        public AsyncBinaryWriter(Stream underlying)
            : this(underlying, Encoding.UTF8, true)
        { }

        public AsyncBinaryWriter(Stream underlying,
            Encoding encoding)
            : this(underlying, encoding, true)
        { }

        public AsyncBinaryWriter(Stream underlying,
            Encoding encoding,
            bool closeOnDispose)
        {
            _underlyingStream = underlying;
            _textEncoding = encoding;
            _closeOnDispose = closeOnDispose;
            _buffer = new MemoryStream();
        }

        public async Task FlushAsync()
        {
            _buffer.Position = 0;

            await _buffer.CopyToAsync(_underlyingStream)
                .ConfigureAwait(false);

            _buffer.Position = 0;
            _buffer.SetLength(0);
        }

        private void WriteByte(byte value)
            => _buffer.WriteByte(value);

        public void Write(Span<byte> data)
        {
            _buffer.Write(data.ToArray(), 0, data.Length);
        }

        public void Write(byte[] bytes,
            int offset = 0, int count = -1)
        {
            if (count < 1)
                count = bytes.Length;

            _buffer.Write(bytes, offset, count);
        }

        public void Write(sbyte value)
            => WriteByte((byte)value);

        public void Write(byte value)
            => WriteByte(value);

        public void Write(short value)
            => Write(BitConverter.GetBytes(value));

        public void Write(ushort value)
            => Write(BitConverter.GetBytes(value));

        public void Write(int value)
            => Write(BitConverter.GetBytes(value));

        public void Write(uint value)
            => Write(BitConverter.GetBytes(value));

        public void Write(long value)
            => Write(BitConverter.GetBytes(value));

        public void Write(ulong value)
            => Write(BitConverter.GetBytes(value));

        public void Write(string value, StringEncoding encoding)
        {
            var bytes = _textEncoding.GetBytes(value);
            switch (encoding)
            {
                case StringEncoding.LengthPrefixedInt8:
                    Write((sbyte)bytes.Length);
                    goto default;
                case StringEncoding.LengthPrefixedUInt8:
                    Write((byte)bytes.Length);
                    goto default;
                case StringEncoding.LengthPrefixedInt16:
                    Write((short)bytes.Length);
                    goto default;
                case StringEncoding.LengthPrefixedUInt16:
                    Write((ushort)bytes.Length);
                    goto default;
                case StringEncoding.LengthPrefixedInt32:
                    Write((int)bytes.Length);
                    goto default;
                case StringEncoding.LengthPrefixedUInt32:
                    Write((uint)bytes.Length);
                    goto default;
                case StringEncoding.LengthPrefixedInt64:
                    Write((long)bytes.Length);
                    goto default;
                case StringEncoding.LengthPrefixedUInt64:
                    Write((ulong)bytes.Length);
                    goto default;
                case StringEncoding.NullTerminated:
                    Array.Resize(ref bytes, bytes.Length + 1);
                    bytes[bytes.Length - 1] = 0;
                    goto default;
                case StringEncoding.FixedLength:
                default:
                    Write(bytes);
                    break;
            }
        }

        public void Dispose()
        {
            if (_closeOnDispose)
            {
                _underlyingStream.Close();
                _underlyingStream.Dispose();
            }

            _buffer?.Dispose();
        }
    }
}
