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
using System.Text;

namespace Tetractic.Formats.PalmPdb
{
    internal sealed class BigEndianBinaryReader : BinaryReader
    {
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The stream does not support reading or is already
        ///     closed.</exception>
        public BigEndianBinaryReader(Stream input)
            : base(input)
        {
        }

        /// <exception cref="ArgumentNullException"><paramref name="input"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="encoding"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The stream does not support reading or is already
        ///     closed.</exception>
        public BigEndianBinaryReader(Stream input, Encoding encoding)
            : base(input, encoding)
        {
        }

        /// <exception cref="ArgumentNullException"><paramref name="input"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="encoding"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The stream does not support reading or is already
        ///     closed.</exception>
        public BigEndianBinaryReader(Stream input, Encoding encoding, bool leaveOpen)
            : base(input, encoding, leaveOpen)
        {
        }

        public override int Read(char[] buffer, int index, int count) => throw new UnreachableException();

        public override char ReadChar() => throw new UnreachableException();

        public override char[] ReadChars(int count) => throw new UnreachableException();

        public override string ReadString() => throw new UnreachableException();

        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        public override short ReadInt16()
        {
            return BinaryPrimitives.ReverseEndianness(base.ReadInt16());
        }

        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        public override ushort ReadUInt16()
        {
            return BinaryPrimitives.ReverseEndianness(base.ReadUInt16());
        }

        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        public override int ReadInt32()
        {
            return BinaryPrimitives.ReverseEndianness(base.ReadInt32());
        }

        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        public override uint ReadUInt32()
        {
            return BinaryPrimitives.ReverseEndianness(base.ReadUInt32());
        }

        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        public override long ReadInt64()
        {
            return BinaryPrimitives.ReverseEndianness(base.ReadInt64());
        }

        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        public override ulong ReadUInt64()
        {
            return BinaryPrimitives.ReverseEndianness(base.ReadUInt64());
        }

        public override float ReadSingle() => throw new UnreachableException();

        public override double ReadDouble() => throw new UnreachableException();

        public override decimal ReadDecimal() => throw new UnreachableException();
    }
}
