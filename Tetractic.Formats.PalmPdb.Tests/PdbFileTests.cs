// Copyright 2021 Carl Reinke
//
// This file is part of a library that is licensed under the terms of GNU Lesser
// General Public License version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Tetractic.Formats.PalmPdb.Tests
{
    public static class PdbFileTests
    {
        [Fact]
        public static void Constructor_StreamIsNull_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new PdbFile(null!));

            Assert.Equal("stream", ex.ParamName);
        }

        [Fact]
        public static void Constructor_StreamIsNotReadable_ThrowsArgumentException()
        {
            using (var stream = new TestStream(Stream.Null, canWrite: true, canSeek: true))
            {
                var ex = Assert.Throws<ArgumentException>(() => new PdbFile(stream));

                Assert.Equal("stream", ex.ParamName);
                Assert.StartsWith("Stream must support reading.", ex.Message);
            }
        }

        [Fact]
        public static void Constructor_StreamIsNotSeekable_ThrowsArgumentException()
        {
            using (var stream = new TestStream(Stream.Null, canRead: true, canWrite: true))
            {
                var ex = Assert.Throws<ArgumentException>(() => new PdbFile(stream));

                Assert.Equal("stream", ex.ParamName);
                Assert.StartsWith("Stream must support seeking.", ex.Message);
            }
        }

        [Fact]
        public static void Constructor_StreamIsNotPositionedAtZero_ThrowsInvalidOperationException()
        {
            using (var stream = new MemoryStream(new byte[] { 0x00 }))
            {
                _ = stream.ReadByte();

                var ex = Assert.Throws<ArgumentException>(() => new PdbFile(stream));

                Assert.Equal("stream", ex.ParamName);
                Assert.StartsWith("Stream must be positioned at 0.", ex.Message);
            }
        }

        [Fact]
        public static void Constructor_StreamIsTooLarge_ThrowsInvalidOperationException()
        {
            using (var stream = new NullStream())
            {
                stream.Position = uint.MaxValue;
                stream.WriteByte(0);
                stream.Position = 0;

                var ex = Assert.Throws<InvalidDataException>(() => new PdbFile(stream));

                Assert.Equal("File is too large.", ex.Message);
            }
        }

        [Fact]
        public static void Constructor_NameIsTruncated_ThrowsEndOfStreamException()
        {
            using (var stream = new NullStream())
            {
                stream.SetLength(31);

                var ex = Assert.Throws<EndOfStreamException>(() => new PdbFile(stream));
            }
        }

        [Fact]
        public static void Constructor_ResourceDatabase_ThrowsNotSupportedException()
        {
            byte[] bytes = new byte[]
            {
                // Name
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // Attributes, Version, CreationTime
                0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // ModificationTime, BackupTime
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // ModificationNumber, AppInfoOffset
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // SortInfoOffset, TypeId
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // CreatorId, UniqueIdSeed
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            };

            using (var stream = new MemoryStream(bytes))
            {
                var ex = Assert.Throws<NotSupportedException>(() => new PdbFile(stream));

                Assert.Equal("Resource database is not supported.", ex.Message);
            }
        }

        [Fact]
        public static void Constructor_MultipleRecordLists_ThrowsInvalidDataException()
        {
            byte[] bytes = new byte[]
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
                // NextRecordListOffset
                0x00, 0x00, 0x01, 0x00,
            };

            using (var stream = new MemoryStream(bytes))
            {
                var ex = Assert.Throws<InvalidDataException>(() => new PdbFile(stream));

                Assert.Equal("Database has multiple record lists.", ex.Message);
            }
        }

        [Fact]
        public static void Constructor_RecordDataOverlapsRecordEntries_ThrowsInvalidDataException()
        {
            byte[] bytes = new byte[]
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
                0x00, 0x00, 0x00, 0x55, 0x00, 0x00, 0x00, 0x00,
            };

            using (var stream = new MemoryStream(bytes))
            {
                var ex = Assert.Throws<InvalidDataException>(() => new PdbFile(stream));

                Assert.Equal("Invalid record data offset.", ex.Message);
            }
        }

        [Fact]
        public static void Constructor_RecordDataOverlapsEndOfFile_ThrowsInvalidDataException()
        {
            byte[] bytes = new byte[]
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
                0x00, 0x00, 0x00, 0x57, 0x00, 0x00, 0x00, 0x00,
            };

            using (var stream = new MemoryStream(bytes))
            {
                var ex = Assert.Throws<InvalidDataException>(() => new PdbFile(stream));

                Assert.Equal("Invalid record data offset.", ex.Message);
            }
        }

        [Fact]
        public static void Constructor_RecordDataOverlapsRecordData_ThrowsInvalidDataException()
        {
            byte[] bytes = new byte[]
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
                0x00, 0x00, 0x00, 0x00, 0x00, 0x02,
                // Record: DataOffset, AttributesAndCategory, UniqueId
                0x00, 0x00, 0x00, 0x60, 0x00, 0x00, 0x00, 0x00,
                // Record: DataOffset, AttributesAndCategory, UniqueId
                0x00, 0x00, 0x00, 0x5E, 0x00, 0x00, 0x00, 0x00,
            };

            using (var stream = new MemoryStream(bytes))
            {
                var ex = Assert.Throws<InvalidDataException>(() => new PdbFile(stream));

                Assert.Equal("Invalid record data offset.", ex.Message);
            }
        }

        [Fact]
        public static void Constructor_AppInfoOverlapsRecordEntries_ThrowsInvalidDataException()
        {
            byte[] bytes = new byte[]
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
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x55,
                // SortInfoOffset, TypeId
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // CreatorId, UniqueIdSeed
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // NextRecordListOffset, RecordCount
                0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
                // Record: DataOffset, AttributesAndCategory, UniqueId
                0x00, 0x00, 0x00, 0x56, 0x00, 0x00, 0x00, 0x00,
            };

            using (var stream = new MemoryStream(bytes))
            {
                var ex = Assert.Throws<InvalidDataException>(() => new PdbFile(stream));

                Assert.Equal("Invalid application info block offset.", ex.Message);
            }
        }

        [Fact]
        public static void Constructor_AppInfoOverlapsEndOfFile_ThrowsInvalidDataException()
        {
            byte[] bytes = new byte[]
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
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x4F,
                // SortInfoOffset, TypeId
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // CreatorId, UniqueIdSeed
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // NextRecordListOffset, RecordCount
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            };

            using (var stream = new MemoryStream(bytes))
            {
                var ex = Assert.Throws<InvalidDataException>(() => new PdbFile(stream));

                Assert.Equal("Invalid application info block offset.", ex.Message);
            }
        }

        [Fact]
        public static void Constructor_AppInfoOverlapsSortInfo_ThrowsInvalidDataException()
        {
            byte[] bytes = new byte[]
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
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x4F,
                // SortInfoOffset, TypeId
                0x00, 0x00, 0x00, 0x4E, 0x00, 0x00, 0x00, 0x00,
                // CreatorId, UniqueIdSeed
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // NextRecordListOffset, RecordCount
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // ...
                0x00,
            };

            using (var stream = new MemoryStream(bytes))
            {
                var ex = Assert.Throws<InvalidDataException>(() => new PdbFile(stream));

                Assert.Equal("Invalid application info block offset.", ex.Message);
            }
        }

        [Fact]
        public static void Constructor_SortInfoOverlapsRecordEntries_ThrowsInvalidDataException()
        {
            byte[] bytes = new byte[]
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
                0x00, 0x00, 0x00, 0x55, 0x00, 0x00, 0x00, 0x00,
                // CreatorId, UniqueIdSeed
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // NextRecordListOffset, RecordCount
                0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
                // Record: DataOffset, AttributesAndCategory, UniqueId
                0x00, 0x00, 0x00, 0x56, 0x00, 0x00, 0x00, 0x00,
            };

            using (var stream = new MemoryStream(bytes))
            {
                var ex = Assert.Throws<InvalidDataException>(() => new PdbFile(stream));

                Assert.Equal("Invalid sort info block offset.", ex.Message);
            }
        }

        [Fact]
        public static void Constructor_SortInfoOverlapsEndOfFile_ThrowsInvalidDataException()
        {
            byte[] bytes = new byte[]
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
                0x00, 0x00, 0x00, 0x4F, 0x00, 0x00, 0x00, 0x00,
                // CreatorId, UniqueIdSeed
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // NextRecordListOffset, RecordCount
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            };

            using (var stream = new MemoryStream(bytes))
            {
                var ex = Assert.Throws<InvalidDataException>(() => new PdbFile(stream));

                Assert.Equal("Invalid sort info block offset.", ex.Message);
            }
        }

        [Fact]
        public static void Attributes_SetWithResourceDatabase_ThrowsNotSupportedException()
        {
            using (var pdbFile = new PdbFile())
            {
                var ex = Assert.Throws<NotSupportedException>(() => pdbFile.Attributes = PdbAttributes.ResourceDatabase);

                Assert.Equal("Resource database is not supported.", ex.Message);
            }
        }

        // TODO: OriginalAppInfoOffset_...

        // TODO: OriginalSortInfoOffset_...

        [Fact]
        public static void AddRecord_MaximumNumberOfRecords_ThrowsInvaldOperationException()
        {
            using (var pdbFile = new PdbFile())
            {
                for (int i = 0; i < ushort.MaxValue; ++i)
                    _ = pdbFile.AddRecord();

                var ex = Assert.Throws<InvalidOperationException>(() => pdbFile.AddRecord());

                Assert.Equal("Database has maximum number of records.", ex.Message);
            }
        }

        [Fact]
        public static void AddRecord_Otherwise_AddsRecordAndReturnsAddedRecord()
        {
            using (var pdbFile = new PdbFile())
            {
                var record = pdbFile.AddRecord();

                Assert.Equal(1, pdbFile.Records.Count);
                Assert.Same(pdbFile.Records[0], record);
            }
        }

        [Fact]
        public static void RemoveRecord_RecordIsNull_ThrowsArgumentNullException()
        {
            using (var pdbFile = new PdbFile())
            {
                var ex = Assert.Throws<ArgumentNullException>(() => pdbFile.RemoveRecord(null!));

                Assert.Equal("record", ex.ParamName);
            }
        }

        [Fact]
        public static void RemoveRecord_RecordDoesNotExist_ReturnsFalse()
        {
            using (var pdbFile1 = new PdbFile())
            using (var pdbFile2 = new PdbFile())
            {
                PdbRecord record = pdbFile1.AddRecord();

                bool result = pdbFile2.RemoveRecord(record);

                Assert.False(result);
                Assert.Equal(1, pdbFile1.Records.Count);
            }
        }

        [Fact]
        public static void RemoveRecord_RecordExists_RemovesRecordAndReturnsTrue()
        {
            using (var pdbFile = new PdbFile())
            {
                PdbRecord record = pdbFile.AddRecord();

                bool result = pdbFile.RemoveRecord(record);

                Assert.True(result);
                Assert.Equal(0, pdbFile.Records.Count);
            }
        }

        [Fact]
        public static void WriteTo_StreamIsNull_ThrowsArgumentNullException()
        {
            using (var pdbFile = new PdbFile())
            {
                var ex = Assert.Throws<ArgumentNullException>(() => pdbFile.WriteTo(null!));

                Assert.Equal("stream", ex.ParamName);
            }
        }

        [Fact]
        public static void WriteTo_StreamIsNotWritable_ThrowsArgumentNullException()
        {
            using (var pdbFile = new PdbFile())
            using (var stream = new TestStream(Stream.Null, canRead: true, canSeek: true))
            {
                var ex = Assert.Throws<ArgumentException>(() => pdbFile.WriteTo(stream));

                Assert.Equal("stream", ex.ParamName);
                Assert.StartsWith("Stream must support writing.", ex.Message);
            }
        }

        [Fact]
        public static void WriteTo_Disposed_ThrowsObjectDisposedException()
        {
            var pdbFile = new PdbFile();

            pdbFile.Dispose();

            var ex = Assert.Throws<ObjectDisposedException>(() => pdbFile.WriteTo(Stream.Null));

            Assert.EndsWith($"'{typeof(PdbFile).FullName}'.", ex.Message);
        }

        [Fact]
        public static void WriteTo_StreamIsNotPositionedAtZero_ThrowsInvalidOperationException()
        {
            using (var stream = new NullStream())
            {
                stream.WriteByte(0);

                using (var pdbFile = new PdbFile())
                {
                    var ex = Assert.Throws<InvalidOperationException>(() => pdbFile.WriteTo(stream));

                    Assert.Equal("Stream must be positioned at 0.", ex.Message);
                }
            }
        }

        [Theory]
        [InlineData(null, null, null, null)]
        [InlineData(new byte[] { 0x12 }, null, null, null)]
        [InlineData(null, new byte[] { 0x34 }, null, null)]
        [InlineData(new byte[] { 0x12 }, new byte[] { 0x34 }, null, null)]
        [InlineData(null, null, new byte[] { 0x56 }, null)]
        [InlineData(new byte[] { 0x12 }, null, new byte[] { 0x56 }, null)]
        [InlineData(null, new byte[] { 0x34 }, new byte[] { 0x56 }, null)]
        [InlineData(new byte[] { 0x12 }, new byte[] { 0x34 }, new byte[] { 0x56 }, null)]
        [InlineData(null, null, new byte[] { 0x56 }, new byte[] { 0x78 })]
        [InlineData(new byte[] { 0x12 }, null, new byte[] { 0x56 }, new byte[] { 0x78 })]
        [InlineData(null, new byte[] { 0x34 }, new byte[] { 0x56 }, new byte[] { 0x78 })]
        [InlineData(new byte[] { 0x12 }, new byte[] { 0x34 }, new byte[] { 0x56 }, new byte[] { 0x78 })]
        public static void WriteTo_VariousParts_Roundtrips(byte[]? appInfo, byte[]? sortInfo, byte[]? record0Data, byte[]? record1Data)
        {
            var recordDatasList = new List<byte[]>();
            if (record0Data != null)
                recordDatasList.Add(record0Data);
            if (record1Data != null)
                recordDatasList.Add(record1Data);
            byte[][] recordDatas = recordDatasList.ToArray();

            using (var stream = new MemoryStream())
            {
                using (var pdb = new PdbFile())
                {
                    pdb.AppInfo = appInfo;
                    pdb.SortInfo = sortInfo;
                    foreach (byte[] recordData in recordDatas)
                    {
                        var record = pdb.AddRecord();
                        using (var dataStream = record.OpenData(FileAccess.Write))
                            dataStream.Write(recordData, 0, recordData.Length);
                    }

                    pdb.WriteTo(stream);
                }

                stream.Position = 0;

                using (var pdb = new PdbFile(stream))
                {
                    Assert.Equal(appInfo, pdb.AppInfo);
                    Assert.Equal(sortInfo, pdb.SortInfo);
                    Assert.Equal(recordDatas.Length, pdb.Records.Count);
                    for (int i = 0; i < recordDatas.Length; ++i)
                    {
                        byte[] recordData = recordDatas[i];
                        var record = pdb.Records[i];

                        byte[] actualRecordData = new byte[(int)record.DataLength];
                        using (var dataStream = record.OpenData(FileAccess.Read))
                        using (var tempStream = new MemoryStream(actualRecordData))
                            dataStream.CopyTo(tempStream);

                        Assert.Equal(recordData, actualRecordData);
                    }
                }
            }
        }
    }
}
