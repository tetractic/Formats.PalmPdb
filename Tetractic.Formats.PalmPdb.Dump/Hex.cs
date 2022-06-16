// Copyright 2021 Carl Reinke
//
// This file is part of a program that is licensed under the terms of GNU Lesser
// General Public License version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using System.IO;

namespace Tetractic.Formats.PalmPdb.Dump
{
    internal static class Hex
    {
        private static readonly TextWriter _bufferedConsoleOut = new StreamWriter(new BufferedStream(Console.OpenStandardOutput(), 0x10000), Console.OutputEncoding);

        /// <exception cref="IOException"/>
        public static void Dump(byte[] bytes, string indent = "", bool writeOffset = false)
        {
            for (int offset = 0; offset < bytes.Length;)
            {
                int length = bytes.Length - offset;
                if (length > 16)
                    length = 16;

                _bufferedConsoleOut.Write(indent);
                if (writeOffset)
                    _bufferedConsoleOut.Write($"{offset:X8}: ");
                for (int i = 0; i < length; ++i)
                    _bufferedConsoleOut.Write($"{bytes[offset + i]:X2} ");
                for (int i = length; i < 16; ++i)
                    _bufferedConsoleOut.Write("   ");
                _bufferedConsoleOut.Write(" ");
                for (int i = 0; i < length; ++i)
                {
                    byte b = bytes[offset + i];
                    _bufferedConsoleOut.Write(b < 32 ? '.' : (char)b);
                }
                _bufferedConsoleOut.WriteLine();

                offset += length;
            }

            _bufferedConsoleOut.Flush();
        }

        /// <exception cref="IOException"/>
        // ExceptionAdjustment: M:System.IO.Stream.Read(System.Byte[],System.Int32,System.Int32) -T:System.NotSupportedException
        public static void Dump(Stream stream, string indent = "", bool writeOffset = false)
        {
            byte[] bytes16 = new byte[16];

            for (int offset = 0; ;)
            {
                int length = 0;

                while (length < 16)
                {
                    int amount = stream.Read(bytes16, 0, 16);
                    if (amount == 0)
                        break;

                    length += amount;
                }

                if (length == 0)
                    break;

                _bufferedConsoleOut.Write(indent);
                if (writeOffset)
                    _bufferedConsoleOut.Write($"{offset:X8}: ");
                for (int i = 0; i < length; ++i)
                    _bufferedConsoleOut.Write($"{bytes16[i]:X2} ");
                for (int i = length; i < 16; ++i)
                    _bufferedConsoleOut.Write("   ");
                _bufferedConsoleOut.Write(" ");
                for (int i = 0; i < length; ++i)
                {
                    byte b = bytes16[i];
                    _bufferedConsoleOut.Write(b < 32 ? '.' : (char)b);
                }
                _bufferedConsoleOut.WriteLine();

                offset += length;
            }

            _bufferedConsoleOut.Flush();
        }
    }
}
