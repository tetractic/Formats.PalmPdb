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
    public static class PdbRecord_WrappedStreamTests
    {
        [Theory]
        [InlineData(FileAccess.Read)]
        [InlineData(FileAccess.ReadWrite)]
        [InlineData(FileAccess.Write)]
        public static void CanRead_Disposed_ReturnsFalse(FileAccess access)
        {
            using (var pdbFile = new PdbFile())
            {
                var record = pdbFile.AddRecord();

                using (var dataStream = record.OpenData(access))
                {
                    dataStream.Dispose();

                    Assert.False(dataStream.CanRead);
                }
            }
        }

        [Theory]
        [InlineData(FileAccess.Read, true)]
        [InlineData(FileAccess.ReadWrite, true)]
        [InlineData(FileAccess.Write, false)]
        public static void CanRead_Otherwise_ReturnsExpectedResult(FileAccess access, bool expectedResult)
        {
            using (var pdbFile = new PdbFile())
            {
                var record = pdbFile.AddRecord();

                using (var dataStream = record.OpenData(access))
                    Assert.Equal(expectedResult, dataStream.CanRead);
            }
        }

        [Theory]
        [InlineData(FileAccess.Read)]
        [InlineData(FileAccess.ReadWrite)]
        [InlineData(FileAccess.Write)]
        public static void CanSeek_Disposed_ReturnsFalse(FileAccess access)
        {
            using (var pdbFile = new PdbFile())
            {
                var record = pdbFile.AddRecord();

                using (var dataStream = record.OpenData(access))
                {
                    dataStream.Dispose();

                    Assert.False(dataStream.CanSeek);
                }
            }
        }

        [Theory]
        [InlineData(FileAccess.Read)]
        [InlineData(FileAccess.ReadWrite)]
        [InlineData(FileAccess.Write)]
        public static void CanSeek_Otherwise_ReturnsTrue(FileAccess access)
        {
            using (var pdbFile = new PdbFile())
            {
                var record = pdbFile.AddRecord();

                using (var dataStream = record.OpenData(access))
                    Assert.True(dataStream.CanSeek);
            }
        }

        [Theory]
        [InlineData(FileAccess.Read)]
        [InlineData(FileAccess.ReadWrite)]
        [InlineData(FileAccess.Write)]
        public static void CanWrite_Disposed_ReturnsFalse(FileAccess access)
        {
            using (var pdbFile = new PdbFile())
            {
                var record = pdbFile.AddRecord();

                using (var dataStream = record.OpenData(access))
                {
                    dataStream.Dispose();

                    Assert.False(dataStream.CanWrite);
                }
            }
        }

        [Theory]
        [InlineData(FileAccess.Read, false)]
        [InlineData(FileAccess.ReadWrite, true)]
        [InlineData(FileAccess.Write, true)]
        public static void CanWrite_Otherwise_ReturnsExpectedResult(FileAccess access, bool expectedResult)
        {
            using (var pdbFile = new PdbFile())
            {
                var record = pdbFile.AddRecord();

                using (var dataStream = record.OpenData(access))
                    Assert.Equal(expectedResult, dataStream.CanWrite);
            }
        }

        [Fact]
        public static void Write_PdbFileDisposed_ThrowObjectDisposedException()
        {
            using (var pdbFile = new PdbFile())
            {
                var record = pdbFile.AddRecord();

                using (var dataStream = record.OpenData(FileAccess.Write))
                {
                    pdbFile.Dispose();

                    var ex = Assert.Throws<ObjectDisposedException>(() => dataStream.Write(new byte[1]));

                    Assert.EndsWith($"'{typeof(PdbRecord).FullName}'.", ex.Message);
                }
            }
        }

        [Fact]
        public static void Write_Stream_ThrowObjectDisposedException()
        {
            using (var pdbFile = new PdbFile())
            {
                var record = pdbFile.AddRecord();

                var dataStream = record.OpenData(FileAccess.Write);

                dataStream.Dispose();

                var ex = Assert.Throws<ObjectDisposedException>(() => dataStream.Write(new byte[1]));

                Assert.EndsWith($"'{typeof(Stream).FullName}'.", ex.Message);
            }
        }
    }
}
