// Copyright 2021 Carl Reinke
//
// This file is part of a library that is licensed under the terms of GNU Lesser
// General Public License version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using System.IO;

namespace Tetractic.Formats.PalmPdb.Tests
{
    internal sealed class TestStream : Stream
    {
        private readonly Stream _stream;

        public TestStream(Stream stream, bool canRead = false, bool canWrite = false, bool canSeek = false)
        {
            _stream = stream;
            CanRead = canRead;
            CanWrite = canWrite;
            CanSeek = canSeek;
        }

        public override bool CanRead { get; }

        public override bool CanSeek { get; }

        public override bool CanWrite { get; }

        public override long Length => CanSeek ? _stream.Length : throw new NotSupportedException();

        public override long Position
        {
            get => CanSeek ? _stream.Position : throw new NotSupportedException();
            set
            {
                if (!CanSeek)
                    throw new NotSupportedException();
                _stream.Position = value;
            }
        }

        public bool Disposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException();
            return _stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
                throw new NotSupportedException();
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            if (!CanSeek)
                throw new NotSupportedException();
            _stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException();
            _stream.Write(buffer, offset, count);
        }
    }
}
