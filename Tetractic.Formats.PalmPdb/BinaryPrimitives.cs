// Copyright 2021 Carl Reinke
//
// This file is part of a library that is licensed under the terms of the GNU
// Lesser General Public License Version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

namespace Tetractic.Formats.PalmPdb
{
    internal static class BinaryPrimitives
    {
        public static short ReverseEndianness(sbyte value)
        {
            return value;
        }

        public static short ReverseEndianness(byte value)
        {
            return value;
        }

        public static short ReverseEndianness(short value)
        {
            return (short)ReverseEndianness((ushort)value);
        }

        public static ushort ReverseEndianness(ushort value)
        {
            return (ushort)((value << 8) | (value >> 8));
        }

        public static int ReverseEndianness(int value)
        {
            return (int)ReverseEndianness((uint)value);
        }

        public static uint ReverseEndianness(uint value)
        {
            return (value << 24) |
                   ((value << 8) & 0x00FF0000) |
                   ((value >> 8) & 0x0000FF00) |
                   (value >> 24);
        }

        public static long ReverseEndianness(long value)
        {
            return (long)ReverseEndianness((ulong)value);
        }

        public static ulong ReverseEndianness(ulong value)
        {
            return (value << 56) |
                   ((value << 40) & 0x00FF0000_00000000) |
                   ((value << 24) & 0x0000FF00_00000000) |
                   ((value <<  8) & 0x000000FF_00000000) |
                   ((value >>  8) & 0x00000000_FF000000) |
                   ((value >> 24) & 0x00000000_00FF0000) |
                   ((value >> 40) & 0x00000000_0000FF00) |
                   (value >> 56);
        }
    }
}
