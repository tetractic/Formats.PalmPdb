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
using Xunit;

namespace Tetractic.Formats.PalmPdb.Tests
{
    public static class PdbRecordTests
    {
        private static readonly byte[] _bytes = new byte[]
        {
            // Name
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // Attributes, Version, CreationTime
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // ModificationTime, BackupTime
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // ModificationNumber, AppInfoOffset
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // SortInfoOffset, TypeId
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // CreatorId, UniqueIdSeed
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // NextRecordListOffset, RecordCount
            0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
            // Record: DataOffset, AttributesAndCategory, UniqueId
            0x00, 0x00, 0x00, 0x56, 0x00, 0x00, 0x00, 0x00,
            // ...
            0x01, 0x02, 0x03,
        };

        [Fact]
        public static void OriginalDataOffset_Always_ReturnsExpectedValue()
        {
            using (var stream = new MemoryStream(_bytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = Assert.Single(pdbFile.Records);

                Assert.Equal(0x00000056u, record.OriginalDataOffset);
            }
        }

        [Fact]
        public static void OriginalDataLength_Always_ReturnsExpectedValue()
        {
            using (var stream = new MemoryStream(_bytes))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = Assert.Single(pdbFile.Records);

                Assert.Equal(3u, record.OriginalDataLength);
            }
        }

        [Fact]
        public static void Attributes_SetWithInvalidValue_ThrowsArgumentOutOfRangeException()
        {
            using (var pdbFile = new PdbFile())
            {
                var record = pdbFile.AddRecord();

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => record.Attributes = (PdbRecordAttributes)0x01);

                Assert.Equal("value", ex.ParamName);
            }
        }

        [Theory]
        [InlineData(0x00)]
        [InlineData(0x0F)]
        public static void Attributes_SetWithValidValue_SetsAttributesAndNotCategory(byte category)
        {
            using (var pdbFile = new PdbFile())
            {
                var record = pdbFile.AddRecord();
                record.Category = category;

                var attributes = (PdbRecordAttributes)0xF0;

                record.Attributes = attributes;

                Assert.Equal(attributes, record.Attributes);
                Assert.Equal(category, record.Category);
            }
        }

        [Fact]
        public static void Category_SetWithInvalidValue_ThrowsArgumentOutOfRangeException()
        {
            using (var pdbFile = new PdbFile())
            {
                var record = pdbFile.AddRecord();

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => record.Category = 0x10);

                Assert.Equal("value", ex.ParamName);
            }
        }

        [Theory]
        [InlineData(PdbRecordAttributes.None)]
        [InlineData((PdbRecordAttributes)0xF0)]
        public static void Category_SetWithValidValue_SetsCategoryAndNotAttributes(PdbRecordAttributes attributes)
        {
            using (var pdbFile = new PdbFile())
            {
                var record = pdbFile.AddRecord();
                record.Attributes = attributes;

                byte category = 0x0F;

                record.Category = category;

                Assert.Equal(0x0F, record.Category);
                Assert.Equal(attributes, record.Attributes);
            }
        }

        [Fact]
        public static void UniqueId_SetWithInvalidValue_ThrowsArgumentOutOfRangeException()
        {
            using (var pdbFile = new PdbFile())
            {
                var record = pdbFile.AddRecord();

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => record.UniqueId = 0x01000000);

                Assert.Equal("value", ex.ParamName);
            }
        }

        // TODO: DataLength_...

        [Fact]
        public static void OpenData_Disposed_ThrowsObjectDisposedException()
        {
            using (var pdbFile = new PdbFile())
            {
                var record = pdbFile.AddRecord();

                record.Dispose();

                var ex = Assert.Throws<ObjectDisposedException>(() => record.OpenData(FileAccess.ReadWrite));

                Assert.EndsWith($"'{typeof(PdbRecord).FullName}'.", ex.Message);
            }
        }

        [Fact]
        public static void OpenData_AlreadyOpen_ThrowsInvalidOperationException()
        {
            using (var pdbFile = new PdbFile())
            {
                var record = pdbFile.AddRecord();

                using (var dataStream = record.OpenData(FileAccess.ReadWrite))
                {
                    var ex = Assert.Throws<InvalidOperationException>(() => record.OpenData(FileAccess.ReadWrite));

                    Assert.Equal("Record data is already open.", ex.Message);
                }
            }
        }

        [Fact]
        public static void OpenData_InvalidAccess_ThrowsArgumentOutOfRangeException()
        {
            using (var pdbFile = new PdbFile())
            {
                var record = pdbFile.AddRecord();

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => record.OpenData((FileAccess)4));

                Assert.Equal("access", ex.ParamName);
            }
        }

        [Theory]
        [InlineData(FileAccess.ReadWrite)]
        [InlineData(FileAccess.Write)]
        public static void OpenData_WriteAccess_CopiesData(FileAccess access)
        {
            byte[] bytes1;

            using (var stream = new MemoryStream())
            using (var pdbFile = new PdbFile())
            {
                var record = pdbFile.AddRecord();

                using (var dataStream = record.OpenData(FileAccess.Write))
                    dataStream.Write(new byte[] { 1, 2, 3 });

                pdbFile.WriteTo(stream);

                bytes1 = stream.ToArray();
            }

            byte[] bytes2 = (byte[])bytes1.Clone();
            byte[] bytes3;

            using (var stream = new MemoryStream(bytes1))
            using (var pdbFile = new PdbFile(stream))
            {
                var record = Assert.Single(pdbFile.Records);

                using (var dataStream = record.OpenData(access))
                    dataStream.Write(new byte[] { 4, 5, 6 });

                using (var tempStream = new MemoryStream())
                {
                    pdbFile.WriteTo(tempStream);

                    bytes3 = tempStream.ToArray();
                }
            }

            Assert.Equal(bytes2, bytes1);
            Assert.NotEqual(bytes2, bytes3);
        }
    }
}
