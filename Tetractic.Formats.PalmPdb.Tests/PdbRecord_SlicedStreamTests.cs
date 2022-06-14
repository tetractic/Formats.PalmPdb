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
using Xunit;

namespace Tetractic.Formats.PalmPdb.Tests
{
    public static class PdbRecord_SlicedStreamTests
    {
        private static readonly byte[] _pdbBytes = GetPdbBytes();

        [Fact]
        public static void CanRead_Disposed_ReturnsFalse()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                {
                    dataStream.Dispose();

                    Assert.False(dataStream.CanRead);
                }
            }
        }

        [Fact]
        public static void CanRead_Otherwise_ReturnsTrue()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                    Assert.True(dataStream.CanRead);
            }
        }

        [Fact]
        public static void CanSeek_Disposed_ReturnsFalse()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                {
                    dataStream.Dispose();

                    Assert.False(dataStream.CanSeek);
                }
            }
        }

        [Fact]
        public static void CanSeek_Otherwise_ReturnsTrue()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                    Assert.True(dataStream.CanSeek);
            }
        }

        [Fact]
        public static void CanWrite_Always_ReturnsFalse()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                    Assert.False(dataStream.CanWrite);
            }
        }

        [Fact]
        public static void Length_Always_ReturnsExpectedValue()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                    Assert.Equal(record.OriginalDataLength, dataStream.Length);
            }
        }

        [Fact]
        public static void Position_SetWithNegativeValue_ThrowsArgumentOutOfRangeException()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                {
                    var ex = Assert.Throws<ArgumentOutOfRangeException>(() => dataStream.Position = -1);

                    Assert.Equal("value", ex.ParamName);
                }
            }
        }

        [Fact]
        public static void Position_SetWithValidValue_ReturnsExpectedValue()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                {
                    dataStream.Position = 1;

                    long actualPosition = dataStream.Position;

                    Assert.Equal(1, actualPosition);
                }
            }
        }

        [Fact]
        public static void Position_SetWithValidValue_ReadReadsFromPosition()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                {
                    dataStream.Position = 1;

                    int result = dataStream.ReadByte();

                    Assert.Equal(2, result);
                }
            }
        }

        [Theory]
        [InlineData(3)]
        [InlineData(4)]
        public static void ReadByte_PositionAtOrAfterEndOfFile_ReturnsNegativeOne(long position)
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                {
                    dataStream.Position = position;

                    int result = dataStream.ReadByte();
                    long actualPosition = dataStream.Position;

                    Assert.Equal(-1, result);
                    Assert.Equal(position, actualPosition);
                }
            }
        }

        [Fact]
        public static void ReadByte_PositionBeforeEndOfFile_ReturnsExpectedValue()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                {
                    int result0 = dataStream.ReadByte();
                    long position0 = dataStream.Position;
                    int result1 = dataStream.ReadByte();
                    long position1 = dataStream.Position;

                    Assert.Equal(1, result0);
                    Assert.Equal(1, position0);
                    Assert.Equal(2, result1);
                    Assert.Equal(2, position1);
                }
            }
        }

        [Fact]
        public static void Read_PdbFileDisposed_ThrowObjectDisposedException()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                {
                    pdbFile.Dispose();

                    var ex = Assert.Throws<ObjectDisposedException>(() => dataStream.Read(new byte[1]));

                    Assert.EndsWith($"'{typeof(PdbFile).FullName}'.", ex.Message);
                }
            }
        }

        [Fact]
        public static void Read_PdbRecordDisposed_ThrowObjectDisposedException()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                {
                    record.Dispose();

                    var ex = Assert.Throws<ObjectDisposedException>(() => dataStream.Read(new byte[1]));

                    Assert.EndsWith($"'{typeof(PdbRecord).FullName}'.", ex.Message);
                }
            }
        }

        [Fact]
        public static void Read_StreamDisposed_ThrowObjectDisposedException()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                var dataStream = record.OpenData(FileAccess.Read);

                dataStream.Dispose();

                var ex = Assert.Throws<ObjectDisposedException>(() => dataStream.Read(new byte[1]));

                Assert.EndsWith($"'{typeof(Stream).FullName}'.", ex.Message);
            }
        }

        [Theory]
        [InlineData(3)]
        [InlineData(4)]
        public static void Read_PositionAtOrAfterEndOfFile_ReturnsZero(long position)
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                {
                    dataStream.Position = position;

                    int result = dataStream.Read(new byte[1], 0, 1);
                    long actualPosition = dataStream.Position;

                    Assert.Equal(0, result);
                    Assert.Equal(position, actualPosition);
                }
            }
        }

        [Fact]
        public static void Read_PositionBeforeEndOfFile_ProducesExpectedResults()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                {
                    byte[] data0 = new byte[1];
                    int result0 = dataStream.Read(data0, 0, 1);
                    long position0 = dataStream.Position;
                    byte[] data1 = new byte[1];
                    int result1 = dataStream.Read(data1, 0, 1);
                    long position1 = dataStream.Position;

                    Assert.Equal(1, result0);
                    Assert.Equal(new byte[] { 1 }, data0);
                    Assert.Equal(1, position0);
                    Assert.Equal(1, result1);
                    Assert.Equal(new byte[] { 2 }, data1);
                    Assert.Equal(2, position1);
                }
            }
        }

        [Theory]
        [InlineData(0, 0, new byte[0], 0)]
        [InlineData(1, 1, new byte[] { 1 }, 1)]
        [InlineData(3, 3, new byte[] { 1, 2, 3 }, 3)]
        [InlineData(4, 3, new byte[] { 1, 2, 3 }, 3)]
        public static void Read_PositionBeforeEndOfFile2_ProducesExpectedResults(int count, int expecteResult, byte[] expecteData, long expectedPosition)
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                {
                    byte[] data = new byte[count];
                    int result = dataStream.Read(data, 0, count);
                    Array.Resize(ref data, result);
                    long position = dataStream.Position;

                    Assert.Equal(expecteResult, result);
                    Assert.Equal(expecteData, data);
                    Assert.Equal(expectedPosition, position);
                }
            }
        }

        [Fact]
        public static void Seek_OriginIsInvalid_ThrowsArgumentOutOfRangeException()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                {
                    var ex = Assert.Throws<ArgumentOutOfRangeException>(() => dataStream.Seek(0, (SeekOrigin)3));

                    Assert.Equal("origin", ex.ParamName);
                }
            }
        }

        [Theory]
        [InlineData(-1, SeekOrigin.Begin)]
        [InlineData(-1, SeekOrigin.Current)]
        [InlineData(-4, SeekOrigin.End)]
        public static void Seek_PositionIsBeforeZero_ThrowsIOException(long offset, SeekOrigin origin)
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                {
                    var ex = Assert.Throws<IOException>(() => dataStream.Seek(offset, origin));

                    Assert.Equal("Specified position is before the beginning of the stream.", ex.Message);
                }
            }
        }

        [Fact]
        public static void Seek_OriginIsBegin_SetsPositionAndReturnsExpectedValue()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                {
                    _ = dataStream.ReadByte();
                    _ = dataStream.ReadByte();

                    long result = dataStream.Seek(1, SeekOrigin.Begin);
                    long position = dataStream.Position;

                    Assert.Equal(1, result);
                    Assert.Equal(1, position);
                }
            }
        }

        [Fact]
        public static void Seek_OriginIsCurrent_SetsPositionAndReturnsExpectedValue()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                {
                    _ = dataStream.ReadByte();
                    _ = dataStream.ReadByte();

                    long result = dataStream.Seek(-1, SeekOrigin.Current);
                    long position = dataStream.Position;

                    Assert.Equal(1, result);
                    Assert.Equal(1, position);
                }
            }
        }

        [Fact]
        public static void Seek_OriginIsEnd_SetsPositionAndReturnsExpectedValue()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                {
                    _ = dataStream.ReadByte();

                    long result = dataStream.Seek(-1, SeekOrigin.End);
                    long position = dataStream.Position;

                    Assert.Equal(2, result);
                    Assert.Equal(2, position);
                }
            }
        }

        [Fact]
        public static void SetLength_Always_ThrowsNotSupportedException()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                    _ = Assert.Throws<NotSupportedException>(() => dataStream.SetLength(0));
            }
        }

        [Fact]
        public static void Write_Always_ThrowsNotSupportedException()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                    _ = Assert.Throws<NotSupportedException>(() => dataStream.Write(Array.Empty<byte>(), 0, 0));
            }
        }

        [Fact]
        public static void WriteByte_Always_ThrowsNotSupportedException()
        {
            using (var stream = new MemoryStream(_pdbBytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = pdbFile.Records[0];

                using (var dataStream = record.OpenData(FileAccess.Read))
                    _ = Assert.Throws<NotSupportedException>(() => dataStream.WriteByte(0));
            }
        }

        private static byte[] GetPdbBytes()
        {
            byte[] bytes;

            using (var stream = new MemoryStream())
            using (var pdbFile = new PdbFile())
            {
                var record0 = pdbFile.AddRecord();

                using (var dataStream = record0.OpenData(FileAccess.Write))
                    dataStream.Write(new byte[] { 1, 2, 3 });

                var record1 = pdbFile.AddRecord();

                using (var dataStream = record1.OpenData(FileAccess.Write))
                    dataStream.Write(new byte[] { 4, 5, 6 });

                pdbFile.WriteTo(stream);

                bytes = stream.ToArray();
            }

            return bytes;
        }
    }
}
