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
using System.Text;
using System.Threading;

namespace Tetractic.Formats.PalmPdb
{
    /// <summary>
    /// Represents a Palm database file.
    /// </summary>
    /// <remarks>
    /// See the "Palm File Format Specification".
    /// </remarks>
    public sealed class PdbFile : IDisposable
    {
        /// <summary>
        /// The maximum number of records that can exist in a <see cref="PdbFile"/>.
        /// </summary>
        public const int MaxRecordCount = ushort.MaxValue;

        internal const int NameLength = 32;

        internal readonly Stream? Stream;

        private readonly bool _leaveOpen;

        private readonly List<PdbRecord> _records;

        private PdbAttributes _attributes;

        private IReadOnlyList<PdbRecord>? _readOnlyRecords;

        private bool _disposed;

        /// <summary>
        /// Initializes a new <see cref="PdbFile"/>.
        /// </summary>
        public PdbFile()
        {
            Name = new byte[NameLength];
            _records = new List<PdbRecord>();
        }

        /// <summary>
        /// Initializes a new <see cref="PdbFile"/> with a specified stream.
        /// </summary>
        /// <param name="stream">The stream that contains the PDB file to be read.</param>
        /// <param name="leaveOpen">Controls whether the stream will remain open after the
        ///     <see cref="PdbFile"/> is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The stream does not support reading and seeking.
        ///     </exception>
        /// <exception cref="ArgumentException">The position of the stream is not 0.</exception>
        /// <exception cref="InvalidDataException">The stream does not contain a valid database.
        ///     </exception>
        /// <exception cref="NotSupportedException">The database is a resource database.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        public PdbFile(Stream stream, bool leaveOpen = false)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("Stream must support reading.", nameof(stream));
            if (!stream.CanSeek)
                throw new ArgumentException("Stream must support seeking.", nameof(stream));

            Stream = stream;
            _leaveOpen = leaveOpen;

            Name = new byte[NameLength];
            _records = new List<PdbRecord>();

            Read(stream);
        }

        /// <summary>
        /// Gets the 32-byte name of the database.  The name must be NUL-terminated.
        /// </summary>
        public byte[] Name { get; }

        /// <summary>
        /// Gets or sets the attributes of the database.
        /// </summary>
        /// <exception cref="NotSupportedException" accessor="set"><paramref name="value"/> has the
        ///     <see cref="PdbAttributes.ResourceDatabase"/> flag set.</exception>
        public PdbAttributes Attributes
        {
            get => _attributes;
            set
            {
                if ((value & PdbAttributes.ResourceDatabase) != 0)
                    throw new NotSupportedException("Resource database is not supported.");

                _attributes = value;
            }
        }

        /// <summary>
        /// Gets or sets the application-specific version number of the database.
        /// </summary>
        public ushort Version { get; set; }

        /// <summary>
        /// Gets or sets the creation date/time of the database.
        /// </summary>
        /// <remarks>
        /// The value is specified to be a number of seconds since 1904-01-01T00:00:00Z but is
        /// usually a number of seconds since 1970-01-01T00:00:00Z.
        /// </remarks>
        public uint CreationTime { get; set; }

        /// <summary>
        /// Gets or sets the date/time of the most recent modification of the database.
        /// </summary>
        /// <remarks>
        /// The value is specified to be a number of seconds since 1904-01-01T00:00:00Z but is
        /// usually a number of seconds since 1970-01-01T00:00:00Z.
        /// </remarks>
        public uint ModificationTime { get; set; }

        /// <summary>
        /// Gets or sets the date/time of the most recent backup of the database.
        /// </summary>
        /// <remarks>
        /// The value is specified to be a number of seconds since 1904-01-01T00:00:00Z but is
        /// usually a number of seconds since 1970-01-01T00:00:00Z.
        /// </remarks>
        public uint BackupTime { get; set; }

        /// <summary>
        /// Gets or sets the modification number of the database.
        /// </summary>
        public uint ModificationNumber { get; set; }

        /// <summary>
        /// Gets the offset from the beginning of the PDB header data to the start of the optional,
        /// application-specific application info block.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The offset is set to 0 for databases that do not include an application info block.
        /// </para>
        /// <para>
        /// <see cref="OriginalAppInfoOffset"/> reflects the value that was loaded from the PDB file
        /// and is not updated when the <see cref="PdbFile"/> instance is mutated.
        /// </para>
        /// </remarks>
        public uint OriginalAppInfoOffset { get; private set; }

        /// <summary>
        /// Gets or sets the optional application-specific application info block.
        /// </summary>
        public byte[]? AppInfo { get; set; }

        /// <summary>
        /// Gets the offset from the beginning of the PDB header data to the start of the optional,
        /// application-specific sort info block.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The offset is set to 0 for databases that do not include a sort info block.
        /// </para>
        /// <para>
        /// <see cref="OriginalSortInfoOffset"/> reflects the value that was loaded from the PDB
        /// file and is not updated when the <see cref="PdbFile"/> instance is mutated.
        /// </para>
        /// </remarks>
        public uint OriginalSortInfoOffset { get; private set; }

        /// <summary>
        /// Gets or sets the optional application-specific sort info block.
        /// </summary>
        public byte[]? SortInfo { get; set; }

        /// <summary>
        /// Gets or sets the database type identifier.
        /// </summary>
        public uint TypeId { get; set; }

        /// <summary>
        /// Gets or sets the database creator identifier.
        /// </summary>
        public uint CreatorId { get; set; }

        /// <summary>
        /// Gets or sets a value used internally by the OS to generate unique identifiers for
        /// records on the device when the database is loaded into the device.
        /// </summary>
        public uint UniqueIdSeed { get; set; }

        /// <summary>
        /// Gets a read-only list of records in the database.
        /// </summary>
        public IReadOnlyList<PdbRecord> Records
        {
            get
            {
                if (_readOnlyRecords == null)
                    _ = Interlocked.CompareExchange(ref _readOnlyRecords, _records.AsReadOnly(), null);

                return _readOnlyRecords;
            }
        }

        /// <summary>
        /// Disposes resources used by the <see cref="PdbFile"/> and also its records.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            if (!_leaveOpen)
                Stream?.Dispose();

            foreach (var record in _records)
                record.Dispose();

            _disposed = true;
        }

        /// <summary>
        /// Adds a new record to the database.
        /// </summary>
        /// <returns>The record that was added.</returns>
        /// <exception cref="InvalidOperationException">The database already has the maximum number
        ///     of records.</exception>
        /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
        public PdbRecord AddRecord()
        {
            if (_records.Count == ushort.MaxValue)
                throw new InvalidOperationException("Database has maximum number of records.");

            ThrowIfDisposed();

            var record = new PdbRecord();

            _records.Add(record);

            return record;
        }

        /// <summary>
        /// Removes a record from the database.
        /// </summary>
        /// <param name="record">The record to remove.</param>
        /// <returns><see langword="false"/> if the record did not exist in the database; otherwise,
        ///     <see langword="true"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="record"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
        public bool RemoveRecord(PdbRecord record)
        {
            if (record is null)
                throw new ArgumentNullException(nameof(record));

            ThrowIfDisposed();

            return _records.Remove(record);
        }

        /// <summary>
        /// Writes the database to a specified stream.
        /// </summary>
        /// <param name="stream">The destination stream.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The stream does not support writing.</exception>
        /// <exception cref="ArgumentException">The position of the stream is not 0.</exception>
        /// <exception cref="OverflowException">The database file would exceed the maximum size (4
        ///     GiB).</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <exception cref="InvalidOperationException">The data stream of a record is open.
        ///     </exception>
        /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
        /// <exception cref="ObjectDisposedException">The stream is already disposed.</exception>
        // ExceptionAdjustment: P:System.IO.Stream.Position get -T:System.NotSupportedException
        // ExceptionAdjustment: M:System.IO.Stream.Write(System.Byte[],System.Int32,System.Int32) -T:System.NotSupportedException
        // ExceptionAdjustment: M:Tetractic.Formats.PalmPdb.PdbWriter.WriteHeader(System.Byte[],Tetractic.Formats.PalmPdb.PdbAttributes,System.UInt16,System.UInt32,System.UInt32,System.UInt32,System.UInt32,System.Nullable{System.Int32},System.Nullable{System.Int32},System.UInt32,System.UInt32,System.UInt32,System.UInt16) -T:System.NotSupportedException
        public void WriteTo(Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite)
                throw new ArgumentException("Stream must support writing.", nameof(stream));

            ThrowIfDisposed();

            using (var writer = new PdbWriter(stream, leaveOpen: true))
            {
                writer.WriteHeader(
                    name: Name,
                    attributes: Attributes,
                    version: Version,
                    creationTime: CreationTime,
                    modificationTime: ModificationTime,
                    backupTime: BackupTime,
                    modificationNumber: ModificationNumber,
                    appInfoLength: AppInfo?.Length,
                    sortInfoLength: SortInfo?.Length,
                    typeId: TypeId,
                    creatorId: CreatorId,
                    uniqueIdSeed: UniqueIdSeed,
                    recordCount: (ushort)Records.Count);

                foreach (var record in Records)
                    writer.WriteRecordEntry(
                        dataLength: record.DataLength,
                        attributes: record.Attributes,
                        category: record.Category,
                        uniqueId: record.UniqueId);

                if (AppInfo != null)
                    writer.WriteAppInfo(AppInfo);

                if (SortInfo != null)
                    writer.WriteSortInfo(SortInfo);

                foreach (var record in Records)
                    using (var dataStream = record.OpenData(FileAccess.Read))
                        _ = writer.WriteRecordData(dataStream);
            }
        }

        internal bool Disposed => _disposed;

        /// <exception cref="ObjectDisposedException"/>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(PdbFile).FullName);
        }

        /// <exception cref="ArgumentException">The position of the stream is not 0.</exception>
        /// <exception cref="NotSupportedException">The stream does not support reading and seeking.
        ///     </exception>
        /// <exception cref="InvalidDataException">The stream does not contain a valid database
        ///     file.</exception>
        /// <exception cref="NotSupportedException">The database is a resource database.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        private void Read(Stream stream)
        {
            if (stream.Position != 0)
                throw new ArgumentException("Stream must be positioned at 0.", nameof(stream));

            // PDB must be smaller than 4 GiB.
            long streamLength = stream.Length;
            if (streamLength > uint.MaxValue)
                throw new InvalidDataException("File is too large.");

            using (var reader = new BigEndianBinaryReader(stream, Encoding.Default, leaveOpen: true))
            {
                int nameLength = reader.Read(Name, 0, Name.Length);
                if (nameLength < Name.Length)
                    throw new EndOfStreamException();

                Attributes = (PdbAttributes)reader.ReadUInt16();
                Version = reader.ReadUInt16();
                CreationTime = reader.ReadUInt32();
                ModificationTime = reader.ReadUInt32();
                BackupTime = reader.ReadUInt32();
                ModificationNumber = reader.ReadUInt32();
                OriginalAppInfoOffset = reader.ReadUInt32();
                OriginalSortInfoOffset = reader.ReadUInt32();
                TypeId = reader.ReadUInt32();
                CreatorId = reader.ReadUInt32();
                UniqueIdSeed = reader.ReadUInt32();

                uint nextRecordListOffset = reader.ReadUInt32();
                if (nextRecordListOffset != 0)
                    throw new InvalidDataException("Database has multiple record lists.");

                ushort recordCount = reader.ReadUInt16();

                const int recordEntryLength = 8;
                long recordEntriesEndOffset = stream.Position + recordEntryLength * recordCount;

                uint infoEndOffset;

                if (recordCount > 0)
                {
                    uint dataOffset = reader.ReadUInt32();

                    infoEndOffset = dataOffset;

                    // First record data must follow record entries.
                    if (dataOffset < recordEntriesEndOffset)
                        throw new InvalidDataException("Invalid record data offset.");

                    for (int i = 0; i < recordCount; ++i)
                    {
                        uint temp = reader.ReadUInt32();
                        byte attributesAndCategory = (byte)(temp >> 24);
                        uint uniqueId = temp & 0xFFFFFF;
                        uint dataEndOffset = i + 1 < recordCount
                            ? reader.ReadUInt32()
                            : (uint)streamLength;

                        // Record data end offset must follow record data offset.
                        if (dataEndOffset < dataOffset)
                            throw new InvalidDataException("Invalid record data offset.");

                        uint dataLength = dataEndOffset - dataOffset;

                        var record = new PdbRecord(file: this, dataOffset, dataLength)
                        {
                            AttributesAndCategory = attributesAndCategory,
                            UniqueId = uniqueId,
                        };

                        _records.Add(record);

                        dataOffset = dataEndOffset;
                    }
                }
                else
                {
                    infoEndOffset = (uint)streamLength;
                }

                // See "Finding the Length of the Sort Information Block" in "Palm File
                // Format Specification".
                if (OriginalSortInfoOffset != 0)
                {
                    // Sort info must follow record entries and must precede record data/
                    // end of file.
                    if (OriginalSortInfoOffset < recordEntriesEndOffset || OriginalSortInfoOffset > infoEndOffset)
                        throw new InvalidDataException("Invalid sort info block offset.");

                    uint sortInfoLength = infoEndOffset - OriginalSortInfoOffset;
                    if (sortInfoLength > int.MaxValue)
                        throw new InvalidDataException("Sort info block is too large.");

                    stream.Position = OriginalSortInfoOffset;
                    SortInfo = reader.ReadBytes((int)sortInfoLength);
                    if (SortInfo.Length < sortInfoLength)
                        throw new EndOfStreamException();

                    infoEndOffset = OriginalSortInfoOffset;
                }

                // See "Finding the Length of the Application Information Block" in "Palm
                // File Format Specification".
                if (OriginalAppInfoOffset != 0)
                {
                    // Application info must follow record entries and must precede sort info/record
                    // data/end of file.
                    if (OriginalAppInfoOffset < recordEntriesEndOffset || OriginalAppInfoOffset > infoEndOffset)
                        throw new InvalidDataException("Invalid application info block offset.");

                    uint appInfoLength = infoEndOffset - OriginalAppInfoOffset;
                    if (appInfoLength > int.MaxValue)
                        throw new InvalidDataException("Application info block is too large.");

                    stream.Position = OriginalAppInfoOffset;
                    AppInfo = reader.ReadBytes((int)appInfoLength);
                    if (AppInfo.Length < appInfoLength)
                        throw new EndOfStreamException();
                }
            }
        }
    }
}
