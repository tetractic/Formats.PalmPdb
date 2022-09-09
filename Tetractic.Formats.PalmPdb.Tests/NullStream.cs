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

namespace Tetractic.Formats.PalmPdb.Tests
{
    internal sealed class NullStream : Stream
    {
        private long _length;
        private long _position;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _position = value;
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (buffer.Length - count < offset)
                throw new ArgumentException("Invalid range in array.");

            if (long.MaxValue - count < Position)
                throw new IOException();

            if (count > _length - _position)
                count = (int)(_length - _position);

            Position += count;

            Array.Clear(buffer, offset, count);

            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
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
                throw new IOException();

            Position = offset;

            return Position;
        }

        public override void SetLength(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _length = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (buffer.Length - count < offset)
                throw new ArgumentException("Invalid range in array.");

            if (long.MaxValue - count < Position)
                throw new IOException();

            Position += count;

            if (_length < Position)
                _length = Position;
        }
    }
}
