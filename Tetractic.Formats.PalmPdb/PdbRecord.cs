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

namespace Tetractic.Formats.PalmPdb
{
    /// <summary>
    /// Represents a Palm database record.
    /// </summary>
    public sealed partial class PdbRecord : IDisposable
    {
        private const byte _attributesMask = 0xF0;

        private const byte _categoryMask = 0x0F;

        private readonly PdbFile? _file;

        internal byte AttributesAndCategory;

        private uint _uniqueId;

        private Stream? _dataStream;

        private bool _dataStreamOpened;

        private bool _disposed;

        internal PdbRecord()
        {
        }

        internal PdbRecord(PdbFile file, uint originalDataOffset, uint originalDataLength)
        {
            _file = file;
            OriginalDataOffset = originalDataOffset;
            OriginalDataLength = originalDataLength;
        }

        /// <summary>
        /// Gets the offset from the beginning of the PDB header data to the start of the record
        /// data.
        /// </summary>
        /// <remarks>
        /// <see cref="OriginalDataOffset"/> reflects the value that was loaded from the PDB file
        /// and is not updated when either the <see cref="PdbRecord"/> or <see cref="PdbFile"/>
        /// instance is mutated.
        /// </remarks>
        public uint OriginalDataOffset { get; }

        /// <summary>
        /// Gets the computed length of the record data.
        /// </summary>
        /// <remarks>
        /// <see cref="OriginalDataLength"/> reflects the value that was computed from the PDB file
        /// and is not updated when either the <see cref="PdbRecord"/> or <see cref="PdbFile"/>
        /// instance is mutated.
        /// </remarks>
        public uint OriginalDataLength { get; }

        /// <summary>
        /// Gets or sets the attributes of the record.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException" accessor="set"><paramref name="value"/>
        ///     has an invalid flag.</exception>
        public PdbRecordAttributes Attributes
        {
            get => (PdbRecordAttributes)(byte)(AttributesAndCategory & _attributesMask);
            set
            {
                if (((byte)value & ~_attributesMask) != 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                AttributesAndCategory = (byte)((AttributesAndCategory & ~_attributesMask) | (byte)value);
            }
        }

        /// <summary>
        /// Gets or sets the category of the record.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException" accessor="set"><paramref name="value"/> is
        ///     greater than 0x0F.</exception>
        public byte Category
        {
            get => (byte)(AttributesAndCategory & _categoryMask);
            set
            {
                if ((value & ~_categoryMask) != 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                AttributesAndCategory = (byte)((AttributesAndCategory & ~_categoryMask) | (byte)value);
            }
        }

        /// <summary>
        /// Gets or sets a 24-bit unique ID for the record.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException" accessor="set"><paramref name="value"/> is
        ///     greater than 0x00FFFFFF.</exception>
        public uint UniqueId
        {
            get => _uniqueId;
            set
            {
                if (value > 0xFFFFFF)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _uniqueId = value;
            }
        }

        /// <summary>
        /// Gets the length of the data of the record.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
        // ExceptionAdjustment: P:System.IO.Stream.Length get -T:System.NotSupportedException
        public uint DataLength
        {
            get
            {
                ThrowIfDisposed();

                return _dataStream != null
                    ? (uint)_dataStream.Length
                    : OriginalDataLength;
            }
        }

        /// <summary>
        /// Disposes resources used by the <see cref="PdbRecord"/>.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            if (_dataStream != null)
                _dataStream.Dispose();

            _disposed = true;
        }

        /// <summary>
        /// Opens the stream containing the data of the record.
        /// </summary>
        /// <param name="access">The access mode.</param>
        /// <returns>The stream containing the data of the record.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="access"/> is invalid.
        ///     </exception>
        /// <exception cref="InvalidOperationException">The data stream is already open.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
        /// <remarks>
        /// If the record belongs to a <see cref="PdbFile"/> that is backed by a stream and
        /// <paramref name="access"/> is <see cref="FileAccess.Write"/> or
        /// <see cref="FileAccess.ReadWrite"/> then the record data will be loaded into memory and
        /// returned so that backing stream will not be modified.
        /// </remarks>
        // ExceptionAdjustment: M:System.IO.Stream.CopyTo(System.IO.Stream) -T:System.NotSupportedException
        // ExceptionAdjustment: P:System.IO.Stream.Position set -T:System.NotSupportedException
        public Stream OpenData(FileAccess access)
        {
            ThrowIfDisposed();

            if (_dataStreamOpened)
                throw new InvalidOperationException("Record data is already open.");

            switch (access)
            {
                case FileAccess.Read:
                    if (_dataStream != null)
                    {
                        _dataStream.Position = 0;

                        return new WrappedStream(this, _dataStream, canRead: true, canWrite: false);
                    }

                    if (_file != null)
                        return new SlicedReadStream(this, OriginalDataOffset, OriginalDataLength);

                    return new WrappedStream(this, Stream.Null, canRead: true, canWrite: false);

                case FileAccess.Write:
                case FileAccess.ReadWrite:
                    if (_dataStream is null)
                    {
                        using (var stream = OpenData(FileAccess.Read))
                        {
                            var dataStream = new MemoryStream(checked((int)OriginalDataLength));

                            stream.CopyTo(dataStream);

                            _dataStream = dataStream;
                        }
                    }

                    _dataStream.Position = 0;

                    return new WrappedStream(this, _dataStream, canRead: access == FileAccess.ReadWrite, canWrite: true);

                default:
                    throw new ArgumentOutOfRangeException(nameof(access));
            }
        }

        /// <exception cref="ObjectDisposedException"/>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(PdbRecord).FullName);
        }
    }
}
