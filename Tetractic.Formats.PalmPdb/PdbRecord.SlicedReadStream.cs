// Copyright 2021 Carl Reinke
//
// This file is part of a library that is licensed under the terms of the GNU
// Lesser General Public License Version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using System.IO;

namespace Tetractic.Formats.PalmPdb
{
    public sealed partial class PdbRecord
    {
        private sealed class SlicedReadStream : Stream
        {
            private readonly PdbRecord _record;

            private readonly PdbFile _file;

            private readonly Stream _stream;

            private readonly uint _offset;

            private readonly uint _length;

            private bool _disposed;

            private long _position;

            internal SlicedReadStream(PdbRecord record, uint offset, uint length)
            {
                _record = record;

                _file = _record._file!;
                _stream = _file.Stream!;
                _offset = offset;
                _length = length;

                _record._dataStreamOpened = true;
            }

            /// <inheritdoc/>
            public override bool CanRead => !_disposed;

            /// <inheritdoc/>
            public override bool CanSeek => !_disposed;

            /// <inheritdoc/>
            public override bool CanWrite => false;

            /// <inheritdoc/>
            public override long Length
            {
                get
                {
                    ThrowIfDisposed();

                    return _length;
                }
            }

            /// <inheritdoc/>
            public override long Position
            {
                get
                {
                    ThrowIfDisposed();

                    return _position;
                }
                set
                {
                    ThrowIfDisposed();

                    if (value < 0)
                        throw new ArgumentOutOfRangeException(nameof(value));

                    _position = value;
                }
            }

            /// <inheritdoc/>
            public override void Flush()
            {
                ThrowIfDisposed();

                lock (_stream)
                    _stream.Flush();
            }

            /// <inheritdoc/>
            public override int Read(byte[] buffer, int offset, int count)
            {
                ThrowIfDisposed();

                if (_position >= _length)
                    return 0;

                uint maxCount = _length - (uint)_position;
                if (count > maxCount)
                    count = (int)maxCount;

                int result;

                long position = _offset + _position;

                lock (_stream)
                {
                    if (_stream.Position != position)
                        _stream.Position = position;

                    result = _stream.Read(buffer, offset, count);
                }

                if (count != 0)
                {
                    if (result == 0)
                        throw new IOException("PDB is truncated.");
                    else
                        _position += result;
                }

                return result;
            }

            /// <inheritdoc/>
            public override int ReadByte()
            {
                ThrowIfDisposed();

                if (_position >= _length)
                    return -1;

                int result;

                long position = _offset + _position;

                lock (_stream)
                {
                    if (_stream.Position != position)
                        _stream.Position = position;

                    result = _stream.ReadByte();
                }

                if (result == -1)
                    throw new IOException("PDB is truncated.");
                else
                    _position += 1;

                return result;
            }

            /// <inheritdoc/>
            public override long Seek(long offset, SeekOrigin origin)
            {
                ThrowIfDisposed();

                switch (origin)
                {
                    case SeekOrigin.Begin:
                        break;

                    case SeekOrigin.Current:
                        offset = _position + offset;
                        break;

                    case SeekOrigin.End:
                        offset = _length + offset;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(origin));
                }

                if (offset < 0)
                    throw new IOException("Specified position is before the beginning of the stream.");

                _position = offset;

                return _position;
            }

            /// <inheritdoc/>
            public override void SetLength(long value) => throw new NotSupportedException();

            /// <inheritdoc/>
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            /// <inheritdoc/>
            public override void WriteByte(byte value) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (_disposed)
                    return;

                base.Dispose(disposing);

                _disposed = true;

                _record._dataStreamOpened = false;
            }

            /// <exception cref="ObjectDisposedException"/>
            private void ThrowIfDisposed()
            {
                if (_file.Disposed)
                    throw new ObjectDisposedException(typeof(PdbFile).FullName);

                if (_record._disposed)
                    throw new ObjectDisposedException(typeof(PdbRecord).FullName);

                if (_disposed)
                    throw new ObjectDisposedException(typeof(Stream).FullName);
            }
        }
    }
}
