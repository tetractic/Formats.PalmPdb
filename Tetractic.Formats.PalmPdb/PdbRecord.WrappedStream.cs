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
        private sealed class WrappedStream : Stream
        {
            private readonly PdbRecord _record;

            private readonly Stream _stream;

            private readonly bool _canRead;

            private readonly bool _canWrite;

            private bool _disposed;

            internal WrappedStream(PdbRecord record, Stream stream, bool canRead, bool canWrite)
            {
                _record = record;
                _stream = stream;
                _canRead = canRead;
                _canWrite = canWrite;

                _record._dataStreamOpened = true;
            }

            /// <inheritdoc/>
            public override bool CanRead => !_disposed && _stream.CanRead && _canRead;

            /// <inheritdoc/>
            public override bool CanSeek => !_disposed && _stream.CanSeek;

            /// <inheritdoc/>
            public override bool CanWrite => !_disposed && _stream.CanWrite && _canWrite;

            /// <inheritdoc/>
            public override long Length
            {
                get
                {
                    ThrowIfDisposed();

                    return _stream.Length;
                }
            }

            /// <inheritdoc/>
            public override long Position
            {
                get
                {
                    ThrowIfDisposed();

                    return _stream.Position;
                }
                set
                {
                    ThrowIfDisposed();

                    _stream.Position = value;
                }
            }

            /// <inheritdoc/>
            public override void Flush()
            {
                ThrowIfDisposed();

                _stream.Flush();
            }

            /// <inheritdoc/>
            public override int Read(byte[] buffer, int offset, int count)
            {
                ThrowIfDisposed();

                if (!_canRead)
                    throw new NotSupportedException("Stream does not support reading.");

                return _stream.Read(buffer, offset, count);
            }

            /// <inheritdoc/>
            public override int ReadByte()
            {
                ThrowIfDisposed();

                if (!_canRead)
                    throw new NotSupportedException("Stream does not support reading.");

                return _stream.ReadByte();
            }

            /// <inheritdoc/>
            public override long Seek(long offset, SeekOrigin origin)
            {
                ThrowIfDisposed();

                return _stream.Seek(offset, origin);
            }

            /// <inheritdoc/>
            public override void SetLength(long value)
            {
                ThrowIfDisposed();

                if (!_canWrite)
                    throw new NotSupportedException("Stream does not support writing.");

                _stream.SetLength(value);
            }

            /// <inheritdoc/>
            public override void Write(byte[] buffer, int offset, int count)
            {
                ThrowIfDisposed();

                if (!_canWrite)
                    throw new NotSupportedException("Stream does not support writing.");

                _stream.Write(buffer, offset, count);
            }

            /// <inheritdoc/>
            public override void WriteByte(byte value)
            {
                ThrowIfDisposed();

                if (!_canWrite)
                    throw new NotSupportedException("Stream does not support writing.");

                _stream.WriteByte(value);
            }

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
                if (_record._disposed)
                    throw new ObjectDisposedException(typeof(PdbRecord).FullName);

                if (_disposed)
                    throw new ObjectDisposedException(typeof(Stream).FullName);
            }
        }
    }
}
