using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Mimic.Common.Networking
{
    public class AsyncBinaryReader : IDisposable
    {
        private const int BlockSize = 1024;

        private readonly Stream _underlyingStream;
        private readonly Encoding _textEncoding;
        private readonly bool _closeOnDispose;

        private readonly byte[] _buffer;
        private int _readHead = 0;
        private int _lastReadSize = 0;

        public int Available => _lastReadSize - _readHead;

        public AsyncBinaryReader(Stream underlying)
            : this(underlying, Encoding.UTF8, true)
        { }

        public AsyncBinaryReader(Stream underlying,
            Encoding encoding)
            : this(underlying, encoding, true)
        { }

        public AsyncBinaryReader(Stream underlying,
            Encoding encoding,
            bool closeOnDispose)
        {
            _underlyingStream = underlying;
            _textEncoding = encoding;
            _closeOnDispose = closeOnDispose;
            _buffer = new byte[BlockSize];
        }

        public async Task<ReadOnlyMemory<byte>> ReadBytesAsync(int count)
        {
            // If we're missing bytes, get the bytes we need
            var _bytesAvailable = _lastReadSize - _readHead;
            if (_bytesAvailable == 0)
                await FillBufferAsync(count - _bytesAvailable)
                    .ConfigureAwait(false);
            else if (_bytesAvailable < count)
                throw new NotSupportedException(
                    "This count would require a cross-packet read");

            _readHead += count;
            return new ReadOnlyMemory<byte>(_buffer, _readHead - count, count);
        }

        public async Task<sbyte> ReadInt8Async()
        {
            var buffer = await ReadBytesAsync(1)
                .ConfigureAwait(false);

            return (sbyte)buffer.Span[0];
        }

        public async Task<byte> ReadUInt8Async()
        {
            var buffer = await ReadBytesAsync(1)
                .ConfigureAwait(false);

            return buffer.Span[0];
        }

        public async Task<short> ReadInt16Async()
        {
            var buffer = await ReadBytesAsync(sizeof(short))
                .ConfigureAwait(false);

            return BinaryPrimitives.ReadInt16LittleEndian(buffer.Span);
        }

        public async Task<ushort> ReadUInt16Async()
        {
            var buffer = await ReadBytesAsync(sizeof(ushort))
                .ConfigureAwait(false);

            return BinaryPrimitives.ReadUInt16LittleEndian(buffer.Span);
        }

        public async Task<int> ReadInt32Async()
        {
            var buffer = await ReadBytesAsync(sizeof(int))
                .ConfigureAwait(false);

            return BinaryPrimitives.ReadInt32LittleEndian(buffer.Span);
        }

        public async Task<uint> ReadUInt32Async()
        {
            var buffer = await ReadBytesAsync(sizeof(uint))
                .ConfigureAwait(false);

            return BinaryPrimitives.ReadUInt32LittleEndian(buffer.Span);
        }

        public async Task<long> ReadInt64Async()
        {
            var buffer = await ReadBytesAsync(sizeof(long))
                .ConfigureAwait(false);

            return BinaryPrimitives.ReadInt64LittleEndian(buffer.Span);
        }

        public async Task<ulong> ReadUInt64Async()
        {
            var buffer = await ReadBytesAsync(sizeof(ulong))
                .ConfigureAwait(false);

            return BinaryPrimitives.ReadUInt64LittleEndian(buffer.Span);
        }

        public async Task<string> ReadStringAsync(StringEncoding encoding,
            int bytes = -1)
        {
            if (encoding == StringEncoding.FixedLength && bytes < 0)
                throw new ArgumentOutOfRangeException(nameof(bytes),
                    "Bytes to read must be greater than zero");

            ReadOnlyMemory<byte> buffer;
            switch (encoding)
            {
                case StringEncoding.LengthPrefixedInt8:
                    bytes = await ReadInt8Async()
                        .ConfigureAwait(false);
                    goto default;
                case StringEncoding.LengthPrefixedUInt8:
                    bytes = await ReadUInt8Async()
                        .ConfigureAwait(false);
                    goto default;
                case StringEncoding.LengthPrefixedInt16:
                    bytes = await ReadInt16Async()
                        .ConfigureAwait(false);
                    goto default;
                case StringEncoding.LengthPrefixedUInt16:
                    bytes = await ReadUInt16Async()
                        .ConfigureAwait(false);
                    goto default;
                case StringEncoding.LengthPrefixedInt32:
                    bytes = await ReadInt32Async()
                        .ConfigureAwait(false);
                    goto default;
                case StringEncoding.LengthPrefixedUInt32:
                case StringEncoding.LengthPrefixedInt64:
                case StringEncoding.LengthPrefixedUInt64:
                    throw new NotImplementedException();
                case StringEncoding.NullTerminated:
                    using (var stream = new MemoryStream())
                    {
                        var index = -1;
                        while (index < 0)
                        {
                            await FillBufferAsync(-1)
                                .ConfigureAwait(false);

                            index = Array.IndexOf(_buffer, 0, 0);

                            if (index < 0)
                                stream.Write(_buffer, 0, _lastReadSize);
                        }

                        // write the last chunk and update the read head
                        stream.Write(_buffer, 0, index);
                        _readHead = index + 1;

                        buffer = new Memory<byte>(stream.GetBuffer(), 0,
                            (int)stream.Length);
                    }
                    break;
                case StringEncoding.FixedLength:
                default:
                    buffer = await ReadBytesAsync(bytes)
                        .ConfigureAwait(false);
                    break;
            }

            return _textEncoding.GetString(buffer.ToArray());
        }

        public void Dispose()
        {
            if (_closeOnDispose)
            {
                _underlyingStream.Close();
                _underlyingStream.Dispose();
            }
        }

        private async Task FillBufferAsync(int count)
        {
            _readHead = 0;

            var offset = 0;
            do
            {
                var read = await _underlyingStream.ReadAsync(
                    _buffer, offset, BlockSize - offset)
                    .ConfigureAwait(false);
                offset += read;
            } while (offset < count);

            _lastReadSize = offset;
        }
    }
}
