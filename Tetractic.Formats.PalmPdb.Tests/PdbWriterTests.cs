// Copyright 2021 Carl Reinke
//
// This file is part of a library that is licensed under the terms of the GNU
// Lesser General Public License Version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Tetractic.Formats.PalmPdb.Tests
{
    public static class PdbWriterTests
    {
        private static readonly string _emptyName = new string('\0', 32);

        [Fact]
        public static void Constructor_StreamIsNull_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new PdbWriter(null!));

            Assert.Equal("stream", ex.ParamName);
        }

        [Fact]
        public static void Constructor_StreamIsNotWritable_ThrowsArgumentException()
        {
            using (var stream = new TestStream(Stream.Null, canRead: true, canSeek: true))
            {
                var ex = Assert.Throws<ArgumentException>(() => new PdbWriter(stream));

                Assert.Equal("stream", ex.ParamName);
                Assert.StartsWith("Stream must support writing.", ex.Message);
            }
        }

        [Fact]
        public static void Dispose_LeaveOpenWasFalse_StreamIsDisposed()
        {
            using (var stream = new TestStream(Stream.Null, canWrite: true))
            {
                using (var writer = new PdbWriter(stream, leaveOpen: false))
                {
                }

                Assert.True(stream.Disposed);
            }
        }

        [Fact]
        public static void Dispose_LeaveOpenWasTrue_StreamIsNotDisposed()
        {
            using (var stream = new TestStream(Stream.Null, canWrite: true))
            {
                using (var writer = new PdbWriter(stream, leaveOpen: true))
                {
                }

                Assert.False(stream.Disposed);
            }
        }

        [Fact]
        public static void WriteHeader_NameIsNull_ThrowsArgumentNullException()
        {
            using (var writer = new PdbWriter(Stream.Null))
            {
                var ex = Assert.Throws<ArgumentNullException>(() =>
                {
                    writer.WriteHeader(
                        name: null!,
                        attributes: PdbAttributes.None,
                        version: 0,
                        creationTime: 0,
                        modificationTime: 0,
                        backupTime: 0,
                        modificationNumber: 0,
                        appInfoLength: 0,
                        sortInfoLength: 0,
                        typeId: 0,
                        creatorId: 0,
                        uniqueIdSeed: 0,
                        recordCount: 0);
                });

                Assert.Equal("name", ex.ParamName);
            }
        }

        [Theory]
        [InlineData(31)]
        [InlineData(33)]
        public static void WriteHeader_NameIsNot32Bytes_ThrowsArgumentNullException(int length)
        {
            using (var writer = new PdbWriter(Stream.Null))
            {
                var ex = Assert.Throws<ArgumentException>(() =>
                {
                    writer.WriteHeader(
                        name: new byte[length],
                        attributes: PdbAttributes.None,
                        version: 0,
                        creationTime: 0,
                        modificationTime: 0,
                        backupTime: 0,
                        modificationNumber: 0,
                        appInfoLength: 0,
                        sortInfoLength: 0,
                        typeId: 0,
                        creatorId: 0,
                        uniqueIdSeed: 0,
                        recordCount: 0);
                });

                Assert.Equal("name", ex.ParamName);
                Assert.StartsWith("Invalid length.", ex.Message);
            }
        }

        [Fact]
        public static void WriteHeader_ResourceDatabase_ThrowsNotSupportedException()
        {
            using (var writer = new PdbWriter(Stream.Null))
            {
                var ex = Assert.Throws<NotSupportedException>(() =>
                {
                    writer.WriteHeader(
                        name: new byte[32],
                        attributes: PdbAttributes.ResourceDatabase,
                        version: 0,
                        creationTime: 0,
                        modificationTime: 0,
                        backupTime: 0,
                        modificationNumber: 0,
                        appInfoLength: 0,
                        sortInfoLength: 0,
                        typeId: 0,
                        creatorId: 0,
                        uniqueIdSeed: 0,
                        recordCount: 0);
                });

                Assert.Equal("Resource database is not supported.", ex.Message);
            }
        }

        [Fact]
        public static void WriteHeader_AppInfoLengthLessThanZero_ThrowsArgumentOutOfRangeException()
        {
            using (var writer = new PdbWriter(Stream.Null))
            {
                var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    writer.WriteHeader(
                        name: new byte[32],
                        attributes: PdbAttributes.None,
                        version: 0,
                        creationTime: 0,
                        modificationTime: 0,
                        backupTime: 0,
                        modificationNumber: 0,
                        appInfoLength: -1,
                        sortInfoLength: 0,
                        typeId: 0,
                        creatorId: 0,
                        uniqueIdSeed: 0,
                        recordCount: 0);
                });

                Assert.Equal("appInfoLength", ex.ParamName);
            }
        }

        [Fact]
        public static void WriteHeader_SortInfoLengthLessThanZero_ThrowsArgumentOutOfRangeException()
        {
            using (var writer = new PdbWriter(Stream.Null))
            {
                var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    writer.WriteHeader(
                        name: new byte[32],
                        attributes: PdbAttributes.None,
                        version: 0,
                        creationTime: 0,
                        modificationTime: 0,
                        backupTime: 0,
                        modificationNumber: 0,
                        appInfoLength: 0,
                        sortInfoLength: -1,
                        typeId: 0,
                        creatorId: 0,
                        uniqueIdSeed: 0,
                        recordCount: 0);
                });

                Assert.Equal("sortInfoLength", ex.ParamName);
            }
        }

        [Fact]
        public static void WriteHeader_InvalidState_ThrowsInvalidOperationException()
        {
            using (var writer = new PdbWriter(Stream.Null))
            {
                writer.WriteHeader(
                    name: new byte[32],
                    attributes: PdbAttributes.None,
                    version: 0,
                    creationTime: 0,
                    modificationTime: 0,
                    backupTime: 0,
                    modificationNumber: 0,
                    appInfoLength: 0,
                    sortInfoLength: 0,
                    typeId: 0,
                    creatorId: 0,
                    uniqueIdSeed: 0,
                    recordCount: 0);

                var ex = Assert.Throws<InvalidOperationException>(() =>
                {
                    writer.WriteHeader(
                        name: new byte[32],
                        attributes: PdbAttributes.None,
                        version: 0,
                        creationTime: 0,
                        modificationTime: 0,
                        backupTime: 0,
                        modificationNumber: 0,
                        appInfoLength: 0,
                        sortInfoLength: 0,
                        typeId: 0,
                        creatorId: 0,
                        uniqueIdSeed: 0,
                        recordCount: 0);
                });

                Assert.Equal("The writer is not in a state that allows writing the header.", ex.Message);
            }
        }

        [Fact]
        public static void WriteHeader_AppInfoAndSortInfoTooLarge_ThrowsOverflowException()
        {
            using (var writer = new PdbWriter(Stream.Null))
            {
                var ex = Assert.Throws<OverflowException>(() =>
                {
                    writer.WriteHeader(
                        name: new byte[32],
                        attributes: PdbAttributes.None,
                        version: 0,
                        creationTime: 0,
                        modificationTime: 0,
                        backupTime: 0,
                        modificationNumber: 0,
                        appInfoLength: int.MaxValue,
                        sortInfoLength: int.MaxValue,
                        typeId: 0,
                        creatorId: 0,
                        uniqueIdSeed: 0,
                        recordCount: 0);
                });

                Assert.Equal("File is too large.", ex.Message);
            }
        }

        [Theory]
        [InlineData(null, PdbAttributes.None, (ushort)0, 0u, 0u, 0u, 0u, 0u, 0u, 0u)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdef", PdbAttributes.None, (ushort)0, 0u, 0u, 0u, 0u, 0u, 0u, 0u)]
        [InlineData(null, ~PdbAttributes.ResourceDatabase, (ushort)0, 0u, 0u, 0u, 0u, 0u, 0u, 0u)]
        [InlineData(null, PdbAttributes.None, (ushort)0xFEDC, 0u, 0u, 0u, 0u, 0u, 0u, 0u)]
        [InlineData(null, PdbAttributes.None, (ushort)0, 0xFEDCBA98u, 0u, 0u, 0u, 0u, 0u, 0u)]
        [InlineData(null, PdbAttributes.None, (ushort)0, 0u, 0xFEDCBA98u, 0u, 0u, 0u, 0u, 0u)]
        [InlineData(null, PdbAttributes.None, (ushort)0, 0u, 0u, 0xFEDCBA98u, 0u, 0u, 0u, 0u)]
        [InlineData(null, PdbAttributes.None, (ushort)0, 0u, 0u, 0u, 0xFEDCBA98u, 0u, 0u, 0u)]
        [InlineData(null, PdbAttributes.None, (ushort)0, 0u, 0u, 0u, 0u, 0xFEDCBA98u, 0u, 0u)]
        [InlineData(null, PdbAttributes.None, (ushort)0, 0u, 0u, 0u, 0u, 0U, 0xFEDCBA98u, 0u)]
        [InlineData(null, PdbAttributes.None, (ushort)0, 0u, 0u, 0u, 0u, 0U, 0u, 0xFEDCBA98u)]
        public static void WriteHeader_VariousValues_Roundtrips(
            string? name,
            PdbAttributes attributes,
            ushort version,
            uint creationTime,
            uint modificationTime,
            uint backupTime,
            uint modificationNumber,
            uint typeId,
            uint creatorId,
            uint uniqueIdSeed)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new PdbWriter(stream, leaveOpen: true))
                    writer.WriteHeader(
                        name: Encoding.ASCII.GetBytes(name ?? _emptyName),
                        attributes: attributes,
                        version: version,
                        creationTime: creationTime,
                        modificationTime: modificationTime,
                        backupTime: backupTime,
                        modificationNumber: modificationNumber,
                        appInfoLength: null,
                        sortInfoLength: null,
                        typeId: typeId,
                        creatorId: creatorId,
                        uniqueIdSeed: uniqueIdSeed,
                        recordCount: 0);

                stream.Position = 0;

                using (var pdbFile = new PdbFile(stream))
                {
                    Assert.Equal(Encoding.ASCII.GetBytes(name ?? _emptyName), pdbFile.Name);
                    Assert.Equal(attributes, pdbFile.Attributes);
                    Assert.Equal(version, pdbFile.Version);
                    Assert.Equal(creationTime, pdbFile.CreationTime);
                    Assert.Equal(modificationTime, pdbFile.ModificationTime);
                    Assert.Equal(backupTime, pdbFile.BackupTime);
                    Assert.Equal(modificationNumber, pdbFile.ModificationNumber);
                    Assert.Equal(typeId, pdbFile.TypeId);
                    Assert.Equal(creatorId, pdbFile.CreatorId);
                    Assert.Equal(uniqueIdSeed, pdbFile.UniqueIdSeed);
                }
            }
        }

        [Fact]
        public static void WriteHeader_StreamIsNotPositionedAtZero_ThrowsInvalidOperationException()
        {
            using (var stream = new NullStream())
            {
                stream.WriteByte(0);

                using (var writer = new PdbWriter(stream))
                {
                    var ex = Assert.Throws<InvalidOperationException>(() =>
                    {
                        writer.WriteHeader(
                            name: new byte[32],
                            attributes: PdbAttributes.None,
                            version: 0,
                            creationTime: 0,
                            modificationTime: 0,
                            backupTime: 0,
                            modificationNumber: 0,
                            appInfoLength: 0,
                            sortInfoLength: 0,
                            typeId: 0,
                            creatorId: 0,
                            uniqueIdSeed: 0,
                            recordCount: 0);
                    });

                    Assert.Equal("Stream must be positioned at 0.", ex.Message);
                }
            }
        }

        [Fact]
        public static void WriteRecordEntry_InvalidAttributes_ThrowsArgumentOutOfRangeException()
        {
            using (var writer = new PdbWriter(Stream.Null))
            {
                writer.WriteHeader(
                    name: new byte[32],
                    attributes: PdbAttributes.None,
                    version: 0,
                    creationTime: 0,
                    modificationTime: 0,
                    backupTime: 0,
                    modificationNumber: 0,
                    appInfoLength: null,
                    sortInfoLength: null,
                    typeId: 0,
                    creatorId: 0,
                    uniqueIdSeed: 0,
                    recordCount: 1);

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    writer.WriteRecordEntry(
                        dataLength: 0,
                        attributes: (PdbRecordAttributes)1,
                        category: 0,
                        uniqueId: 0);
                });

                Assert.Equal("attributes", ex.ParamName);
            }
        }

        [Fact]
        public static void WriteRecordEntry_InvalidCategory_ThrowsArgumentOutOfRangeException()
        {
            using (var writer = new PdbWriter(Stream.Null))
            {
                writer.WriteHeader(
                    name: new byte[32],
                    attributes: PdbAttributes.None,
                    version: 0,
                    creationTime: 0,
                    modificationTime: 0,
                    backupTime: 0,
                    modificationNumber: 0,
                    appInfoLength: null,
                    sortInfoLength: null,
                    typeId: 0,
                    creatorId: 0,
                    uniqueIdSeed: 0,
                    recordCount: 1);

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    writer.WriteRecordEntry(
                        dataLength: 0,
                        attributes: PdbRecordAttributes.None,
                        category: 0x10,
                        uniqueId: 0);
                });

                Assert.Equal("category", ex.ParamName);
            }
        }

        [Fact]
        public static void WriteRecordEntry_InvalidUniqueId_ThrowsArgumentOutOfRangeException()
        {
            using (var writer = new PdbWriter(Stream.Null))
            {
                writer.WriteHeader(
                    name: new byte[32],
                    attributes: PdbAttributes.None,
                    version: 0,
                    creationTime: 0,
                    modificationTime: 0,
                    backupTime: 0,
                    modificationNumber: 0,
                    appInfoLength: null,
                    sortInfoLength: null,
                    typeId: 0,
                    creatorId: 0,
                    uniqueIdSeed: 0,
                    recordCount: 1);

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    writer.WriteRecordEntry(
                        dataLength: 0,
                        attributes: PdbRecordAttributes.None,
                        category: 0,
                        uniqueId: 0x01000000);
                });

                Assert.Equal("uniqueId", ex.ParamName);
            }
        }

        [Fact]
        public static void WriteRecordEntry_InvalidState_ThrowsInvalidOperationException()
        {
            using (var writer = new PdbWriter(Stream.Null))
            {
                var ex = Assert.Throws<InvalidOperationException>(() =>
                {
                    writer.WriteRecordEntry(
                        dataLength: 0,
                        attributes: PdbRecordAttributes.None,
                        category: 0,
                        uniqueId: 0);
                });

                Assert.Equal("The writer is not in a state that allows writing a record entry.", ex.Message);
            }
        }

        [Fact]
        public static void WriteRecordEntry_DataTooLarge_ThrowsOverflowException()
        {
            using (var writer = new PdbWriter(Stream.Null))
            {
                writer.WriteHeader(
                    name: new byte[32],
                    attributes: PdbAttributes.None,
                    version: 0,
                    creationTime: 0,
                    modificationTime: 0,
                    backupTime: 0,
                    modificationNumber: 0,
                    appInfoLength: null,
                    sortInfoLength: null,
                    typeId: 0,
                    creatorId: 0,
                    uniqueIdSeed: 0,
                    recordCount: 1);

                var ex = Assert.Throws<OverflowException>(() =>
                {
                    writer.WriteRecordEntry(
                        dataLength: uint.MaxValue,
                        attributes: PdbRecordAttributes.None,
                        category: 0,
                        uniqueId: 0);
                });

                Assert.Equal("File is too large.", ex.Message);
            }
        }

        [Theory]
        [InlineData(0u, PdbRecordAttributes.None, 0, 0)]
        [InlineData(0x10000u, PdbRecordAttributes.None, 0, 0)]
        [InlineData(0u, (PdbRecordAttributes)0xF0, 0, 0)]
        [InlineData(0u, PdbRecordAttributes.None, 0xF, 0)]
        [InlineData(0u, PdbRecordAttributes.None, 0, 0xFFFFFF)]
        public static void WriteRecordEntry_VariousData_Roundtrips(
            uint dataLength,
            PdbRecordAttributes attributes,
            byte category,
            uint uniqueId)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new PdbWriter(stream, leaveOpen: true))
                {
                    writer.WriteHeader(
                        name: new byte[32],
                        attributes: PdbAttributes.None,
                        version: 0,
                        creationTime: 0,
                        modificationTime: 0,
                        backupTime: 0,
                        modificationNumber: 0,
                        appInfoLength: null,
                        sortInfoLength: null,
                        typeId: 0,
                        creatorId: 0,
                        uniqueIdSeed: 0,
                        recordCount: 1);

                    writer.WriteRecordEntry(
                        dataLength: dataLength,
                        attributes: attributes,
                        category: category,
                        uniqueId: uniqueId);

                    using (var dataStream = new NullStream())
                    {
                        dataStream.SetLength(dataLength);

                        _ = writer.WriteRecordData(dataStream);
                    }
                }

                stream.Position = 0;

                using (var pdb = new PdbFile(stream))
                {
                    var record = Assert.Single(pdb.Records);

                    Assert.Equal(dataLength, record.DataLength);
                    Assert.Equal(attributes, record.Attributes);
                    Assert.Equal(category, record.Category);
                    Assert.Equal(uniqueId, record.UniqueId);
                }
            }
        }

        public static readonly TheoryData<byte[]?, byte[]?, byte[]?, byte[]?> WriteRecordEntry_VariousParts_Data = new TheoryData<byte[]?, byte[]?, byte[]?, byte[]?>()
        {
            { null, null, null, null },
            { new byte[] { 0x12 }, null, null, null },
            { null, new byte[] { 0x34 }, null, null },
            { new byte[] { 0x12 }, new byte[] { 0x34 }, null, null },
            { null, null, new byte[] { 0x56 }, null },
            { new byte[] { 0x12 }, null, new byte[] { 0x56 }, null },
            { null, new byte[] { 0x34 }, new byte[] { 0x56 }, null },
            { new byte[] { 0x12 }, new byte[] { 0x34 }, new byte[] { 0x56 }, null },
            { null, null, new byte[] { 0x56 }, new byte[] { 0x78 } },
            { new byte[] { 0x12 }, null, new byte[] { 0x56 }, new byte[] { 0x78 } },
            { null, new byte[] { 0x34 }, new byte[] { 0x56 }, new byte[] { 0x78 } },
            { new byte[] { 0x12 }, new byte[] { 0x34 }, new byte[] { 0x56 }, new byte[] { 0x78 } },
        };

        [Theory]
        [MemberData(nameof(WriteRecordEntry_VariousParts_Data))]
        public static void WriteRecordEntry_VariousPartsAndDataSpan_Roundtrips(byte[]? appInfo, byte[]? sortInfo, byte[]? record0Data, byte[]? record1Data)
        {
            var recordDatasList = new List<byte[]>();
            if (record0Data != null)
                recordDatasList.Add(record0Data);
            if (record1Data != null)
                recordDatasList.Add(record1Data);
            byte[][] recordDatas = recordDatasList.ToArray();

            using (var stream = new MemoryStream())
            {
                using (var writer = new PdbWriter(stream, leaveOpen: true))
                {
                    writer.WriteHeader(
                        name: new byte[32],
                        attributes: PdbAttributes.None,
                        version: 0,
                        creationTime: 0,
                        modificationTime: 0,
                        backupTime: 0,
                        modificationNumber: 0,
                        appInfoLength: appInfo?.Length,
                        sortInfoLength: sortInfo?.Length,
                        typeId: 0,
                        creatorId: 0,
                        uniqueIdSeed: 0,
                        recordCount: (ushort)recordDatas.Length);

                    for (int i = 0; i < recordDatas.Length; ++i)
                    {
                        writer.WriteRecordEntry(
                            dataLength: (uint)recordDatas[i].Length,
                            attributes: PdbRecordAttributes.None,
                            category: 0,
                            uniqueId: 0);
                    }

                    if (appInfo != null)
                        writer.WriteAppInfo(appInfo);

                    if (sortInfo != null)
                        writer.WriteSortInfo(sortInfo);

                    for (int i = 0; i < recordDatas.Length; ++i)
                    {
                        byte[] recordData = recordDatas[i];

                        writer.WriteRecordData(recordData);
                    }
                }

                stream.Position = 0;

                AssertVariousParts(appInfo, sortInfo, recordDatas, stream);
            }
        }

        [Theory]
        [MemberData(nameof(WriteRecordEntry_VariousParts_Data))]
        public static void WriteRecordEntry_VariousPartsAndDataStream_Roundtrips(byte[]? appInfo, byte[]? sortInfo, byte[]? record0Data, byte[]? record1Data)
        {
            var recordDatasList = new List<byte[]>();
            if (record0Data != null)
                recordDatasList.Add(record0Data);
            if (record1Data != null)
                recordDatasList.Add(record1Data);
            byte[][] recordDatas = recordDatasList.ToArray();

            using (var stream = new MemoryStream())
            {
                using (var writer = new PdbWriter(stream, leaveOpen: true))
                {
                    writer.WriteHeader(
                        name: new byte[32],
                        attributes: PdbAttributes.None,
                        version: 0,
                        creationTime: 0,
                        modificationTime: 0,
                        backupTime: 0,
                        modificationNumber: 0,
                        appInfoLength: appInfo?.Length,
                        sortInfoLength: sortInfo?.Length,
                        typeId: 0,
                        creatorId: 0,
                        uniqueIdSeed: 0,
                        recordCount: (ushort)recordDatas.Length);

                    for (int i = 0; i < recordDatas.Length; ++i)
                    {
                        writer.WriteRecordEntry(
                            dataLength: (uint)recordDatas[i].Length,
                            attributes: PdbRecordAttributes.None,
                            category: 0,
                            uniqueId: 0);
                    }

                    if (appInfo != null)
                        writer.WriteAppInfo(appInfo);

                    if (sortInfo != null)
                        writer.WriteSortInfo(sortInfo);

                    for (int i = 0; i < recordDatas.Length; ++i)
                    {
                        byte[] recordData = recordDatas[i];

                        _ = writer.WriteRecordData(new MemoryStream(recordData));
                    }
                }

                stream.Position = 0;

                AssertVariousParts(appInfo, sortInfo, recordDatas, stream);
            }
        }

        [Theory]
        [MemberData(nameof(WriteRecordEntry_VariousParts_Data))]
        public static void WriteRecordEntry_DeferredWithVariousPartsAndDataSpan_Roundtrips(byte[]? appInfo, byte[]? sortInfo, byte[]? record0Data, byte[]? record1Data)
        {
            var recordDatasList = new List<byte[]>();
            if (record0Data != null)
                recordDatasList.Add(record0Data);
            if (record1Data != null)
                recordDatasList.Add(record1Data);
            byte[][] recordDatas = recordDatasList.ToArray();

            using (var stream = new MemoryStream())
            {
                using (var writer = new PdbWriter(stream, leaveOpen: true))
                {
                    writer.WriteHeader(
                        name: new byte[32],
                        attributes: PdbAttributes.None,
                        version: 0,
                        creationTime: 0,
                        modificationTime: 0,
                        backupTime: 0,
                        modificationNumber: 0,
                        appInfoLength: appInfo?.Length,
                        sortInfoLength: sortInfo?.Length,
                        typeId: 0,
                        creatorId: 0,
                        uniqueIdSeed: 0,
                        recordCount: (ushort)recordDatas.Length);

                    if (appInfo != null)
                        writer.WriteAppInfo(appInfo);

                    if (sortInfo != null)
                        writer.WriteSortInfo(sortInfo);

                    for (int i = 0; i < recordDatas.Length; ++i)
                    {
                        byte[] recordData = recordDatas[i];

                        writer.WriteRecordData(recordData);
                    }

                    for (int i = 0; i < recordDatas.Length; ++i)
                    {
                        writer.WriteRecordEntry(
                            dataLength: (uint)recordDatas[i].Length,
                            attributes: PdbRecordAttributes.None,
                            category: 0,
                            uniqueId: 0);
                    }
                }

                stream.Position = 0;

                AssertVariousParts(appInfo, sortInfo, recordDatas, stream);
            }
        }

        [Theory]
        [MemberData(nameof(WriteRecordEntry_VariousParts_Data))]
        public static void WriteRecordEntry_DeferredWithVariousPartsAndDataStream_Roundtrips(byte[]? appInfo, byte[]? sortInfo, byte[]? record0Data, byte[]? record1Data)
        {
            var recordDatasList = new List<byte[]>();
            if (record0Data != null)
                recordDatasList.Add(record0Data);
            if (record1Data != null)
                recordDatasList.Add(record1Data);
            byte[][] recordDatas = recordDatasList.ToArray();

            using (var stream = new MemoryStream())
            {
                using (var writer = new PdbWriter(stream, leaveOpen: true))
                {
                    writer.WriteHeader(
                        name: new byte[32],
                        attributes: PdbAttributes.None,
                        version: 0,
                        creationTime: 0,
                        modificationTime: 0,
                        backupTime: 0,
                        modificationNumber: 0,
                        appInfoLength: appInfo?.Length,
                        sortInfoLength: sortInfo?.Length,
                        typeId: 0,
                        creatorId: 0,
                        uniqueIdSeed: 0,
                        recordCount: (ushort)recordDatas.Length);

                    if (appInfo != null)
                        writer.WriteAppInfo(appInfo);

                    if (sortInfo != null)
                        writer.WriteSortInfo(sortInfo);

                    uint[] recordDataLengths = new uint[recordDatas.Length];

                    for (int i = 0; i < recordDatas.Length; ++i)
                    {
                        byte[] recordData = recordDatas[i];

                        recordDataLengths[i] = writer.WriteRecordData(new MemoryStream(recordData));
                    }

                    for (int i = 0; i < recordDatas.Length; ++i)
                    {
                        writer.WriteRecordEntry(
                            dataLength: recordDataLengths[i],
                            attributes: PdbRecordAttributes.None,
                            category: 0,
                            uniqueId: 0);
                    }
                }

                stream.Position = 0;

                AssertVariousParts(appInfo, sortInfo, recordDatas, stream);
            }
        }

        private static void AssertVariousParts(byte[]? appInfo, byte[]? sortInfo, byte[][] recordDatas, MemoryStream stream)
        {
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

        [Fact]
        public static void WriteAppInfo_DeferredRecordEntriesAndStreamNotSeekable_ThrowsInvalidOperationException()
        {
            using (var stream = new TestStream(Stream.Null, canRead: true, canWrite: true))
            using (var writer = new PdbWriter(stream))
            {
                writer.WriteHeader(
                    name: new byte[32],
                    attributes: PdbAttributes.None,
                    version: 0,
                    creationTime: 0,
                    modificationTime: 0,
                    backupTime: 0,
                    modificationNumber: 0,
                    appInfoLength: 1,
                    sortInfoLength: null,
                    typeId: 0,
                    creatorId: 0,
                    uniqueIdSeed: 0,
                    recordCount: 1);

                var ex = Assert.Throws<InvalidOperationException>(() => writer.WriteAppInfo(new byte[1]));

                Assert.Equal("The stream must support seeking to defer writing the record entries.", ex.Message);
            }
        }

        [Fact]
        public static void WriteAppInfo_InvalidState_ThrowsInvalidOperationException()
        {
            using (var writer = new PdbWriter(Stream.Null))
            {
                var ex = Assert.Throws<InvalidOperationException>(() => writer.WriteAppInfo(new byte[1]));

                Assert.Equal("The writer is not in a state that allows writing the application info.", ex.Message);
            }
        }

        [Fact]
        public static void WriteAppInfo_MismatchedLength_ThrowsInvalidOperationException()
        {
            using (var stream = new NullStream())
            using (var writer = new PdbWriter(stream))
            {
                writer.WriteHeader(
                    name: new byte[32],
                    attributes: PdbAttributes.None,
                    version: 0,
                    creationTime: 0,
                    modificationTime: 0,
                    backupTime: 0,
                    modificationNumber: 0,
                    appInfoLength: 1,
                    sortInfoLength: null,
                    typeId: 0,
                    creatorId: 0,
                    uniqueIdSeed: 0,
                    recordCount: 1);

                var ex = Assert.Throws<InvalidOperationException>(() => writer.WriteAppInfo(Array.Empty<byte>()));

                Assert.Equal("The length does not match the length specified when writing the header.", ex.Message);
            }
        }

        [Fact]
        public static void WriteSortInfo_DeferredRecordEntriesAndStreamNotSeekable_ThrowsInvalidOperationException()
        {
            using (var stream = new TestStream(Stream.Null, canRead: true, canWrite: true))
            using (var writer = new PdbWriter(stream))
            {
                writer.WriteHeader(
                    name: new byte[32],
                    attributes: PdbAttributes.None,
                    version: 0,
                    creationTime: 0,
                    modificationTime: 0,
                    backupTime: 0,
                    modificationNumber: 0,
                    appInfoLength: null,
                    sortInfoLength: 1,
                    typeId: 0,
                    creatorId: 0,
                    uniqueIdSeed: 0,
                    recordCount: 1);

                var ex = Assert.Throws<InvalidOperationException>(() => writer.WriteSortInfo(new byte[1]));

                Assert.Equal("The stream must support seeking to defer writing the record entries.", ex.Message);
            }
        }

        [Fact]
        public static void WriteSortInfo_InvalidState_ThrowsInvalidOperationException()
        {
            using (var writer = new PdbWriter(Stream.Null))
            {
                var ex = Assert.Throws<InvalidOperationException>(() => writer.WriteSortInfo(new byte[1]));

                Assert.Equal("The writer is not in a state that allows writing the sort info.", ex.Message);
            }
        }

        [Fact]
        public static void WriteSortInfo_MismatchedLength_ThrowsInvalidOperationException()
        {
            using (var stream = new NullStream())
            using (var writer = new PdbWriter(stream))
            {
                writer.WriteHeader(
                    name: new byte[32],
                    attributes: PdbAttributes.None,
                    version: 0,
                    creationTime: 0,
                    modificationTime: 0,
                    backupTime: 0,
                    modificationNumber: 0,
                    appInfoLength: null,
                    sortInfoLength: 1,
                    typeId: 0,
                    creatorId: 0,
                    uniqueIdSeed: 0,
                    recordCount: 1);

                var ex = Assert.Throws<InvalidOperationException>(() => writer.WriteSortInfo(Array.Empty<byte>()));

                Assert.Equal("The length does not match the length specified when writing the header.", ex.Message);
            }
        }

        [Fact]
        public static void WriteRecordData_DataSourceIsNull_ThrowsArgumentNullException()
        {
            using (var stream = new NullStream())
            using (var writer = new PdbWriter(stream))
            {
                writer.WriteHeader(
                    name: new byte[32],
                    attributes: PdbAttributes.None,
                    version: 0,
                    creationTime: 0,
                    modificationTime: 0,
                    backupTime: 0,
                    modificationNumber: 0,
                    appInfoLength: null,
                    sortInfoLength: null,
                    typeId: 0,
                    creatorId: 0,
                    uniqueIdSeed: 0,
                    recordCount: 1);

                var ex = Assert.Throws<ArgumentNullException>(() => writer.WriteRecordData((Stream)null!));

                Assert.Equal("dataSource", ex.ParamName);
            }
        }

        [Fact]
        public static void WriteRecordData_DataSourceDoesNotSupportReading_ThrowsArgumentException()
        {
            using (var stream = new NullStream())
            using (var writer = new PdbWriter(stream))
            {
                writer.WriteHeader(
                    name: new byte[32],
                    attributes: PdbAttributes.None,
                    version: 0,
                    creationTime: 0,
                    modificationTime: 0,
                    backupTime: 0,
                    modificationNumber: 0,
                    appInfoLength: null,
                    sortInfoLength: null,
                    typeId: 0,
                    creatorId: 0,
                    uniqueIdSeed: 0,
                    recordCount: 1);

                using (var dataSource = new TestStream(Stream.Null, canWrite: true, canSeek: true))
                {
                    var ex = Assert.Throws<ArgumentException>(() => writer.WriteRecordData(dataSource));

                    Assert.Equal("dataSource", ex.ParamName);
                    Assert.StartsWith("The stream must be support reading.", ex.Message);
                }
            }
        }

        [Fact]
        public static void WriteRecordData_DataTooLarge_ThrowsOverflowException()
        {
            using (var writer = new PdbWriter(new NullStream()))
            {
                writer.WriteHeader(
                    name: new byte[32],
                    attributes: PdbAttributes.None,
                    version: 0,
                    creationTime: 0,
                    modificationTime: 0,
                    backupTime: 0,
                    modificationNumber: 0,
                    appInfoLength: null,
                    sortInfoLength: null,
                    typeId: 0,
                    creatorId: 0,
                    uniqueIdSeed: 0,
                    recordCount: 1);

                using (var nullStream = new NullStream())
                {
                    nullStream.SetLength(long.MaxValue);

                    var ex = Assert.Throws<OverflowException>(() => _ = writer.WriteRecordData(nullStream));

                    Assert.Equal("File is too large.", ex.Message);
                }
            }
        }

        [Fact]
        public static void WriteRecordData_DeferredRecordEntriesAndStreamNotSeekable_ThrowsInvalidOperationException()
        {
            using (var stream = new TestStream(Stream.Null, canRead: true, canWrite: true))
            using (var writer = new PdbWriter(stream))
            {
                writer.WriteHeader(
                    name: new byte[32],
                    attributes: PdbAttributes.None,
                    version: 0,
                    creationTime: 0,
                    modificationTime: 0,
                    backupTime: 0,
                    modificationNumber: 0,
                    appInfoLength: null,
                    sortInfoLength: null,
                    typeId: 0,
                    creatorId: 0,
                    uniqueIdSeed: 0,
                    recordCount: 1);

                var ex = Assert.Throws<InvalidOperationException>(() => writer.WriteRecordData(Array.Empty<byte>()));

                Assert.Equal("The stream must support seeking to defer writing the record entries.", ex.Message);
            }
        }

        [Fact]
        public static void WriteRecordData_InvalidState_ThrowsInvalidOperationException()
        {
            using (var writer = new PdbWriter(Stream.Null))
            {
                var ex = Assert.Throws<InvalidOperationException>(() => writer.WriteRecordData(Array.Empty<byte>()));

                Assert.Equal("The writer is not in a state that allows writing record data.", ex.Message);
            }
        }
    }
}
