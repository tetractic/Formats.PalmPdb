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
    internal sealed class BigEndianBinaryWriter : BinaryWriter
    {
        /// <exception cref="ArgumentNullException"><paramref name="output"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The stream does not support writing or is already
        ///     closed.</exception>
        public BigEndianBinaryWriter(Stream output)
            : base(output)
        {
        }

        /// <exception cref="ArgumentNullException"><paramref name="output"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="encoding"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The stream does not support writing or is already
        ///     closed.</exception>
        public BigEndianBinaryWriter(Stream output, Encoding encoding)
            : base(output, encoding)
        {
        }

        /// <exception cref="ArgumentNullException"><paramref name="output"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="encoding"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The stream does not support writing or is already
        ///     closed.</exception>
        public BigEndianBinaryWriter(Stream output, Encoding encoding, bool leaveOpen)
            : base(output, encoding, leaveOpen)
        {
        }

        public override void Write(char ch) => throw new UnreachableException();

        public override void Write(char[] chars) => throw new UnreachableException();

        public override void Write(char[] chars, int index, int count) => throw new UnreachableException();

        public override void Write(string value) => throw new UnreachableException();

        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        public override void Write(short value)
        {
            base.Write(BinaryPrimitives.ReverseEndianness(value));
        }

        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        public override void Write(ushort value)
        {
            base.Write(BinaryPrimitives.ReverseEndianness(value));
        }

        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        public override void Write(int value)
        {
            base.Write(BinaryPrimitives.ReverseEndianness(value));
        }

        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        public override void Write(uint value)
        {
            base.Write(BinaryPrimitives.ReverseEndianness(value));
        }

        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        public override void Write(long value)
        {
            base.Write(BinaryPrimitives.ReverseEndianness(value));
        }

        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        public override void Write(ulong value)
        {
            base.Write(BinaryPrimitives.ReverseEndianness(value));
        }

        public override void Write(float value) => throw new UnreachableException();

        public override void Write(double value) => throw new UnreachableException();

        public override void Write(decimal value) => throw new UnreachableException();
    }
}
