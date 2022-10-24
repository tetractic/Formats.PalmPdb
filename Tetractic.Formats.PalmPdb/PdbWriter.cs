// Copyright 2021 Carl Reinke
//
// This file is part of a library that is licensed under the terms of the GNU
// Lesser General Public License Version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Tetractic.Formats.PalmPdb
{
    /// <summary>
    /// A writer that writes a PDB file to a stream in parts.
    /// </summary>
    public sealed class PdbWriter : IDisposable
    {
        private const uint _headerLength = 72;
        private const uint _recordListHeaderLength = 6;
        private const uint _recordEntryLength = 8;
        private const uint _recordListFooterLength = 2;

        private readonly Stream _stream;

        private readonly bool _leaveOpen;

        private readonly BigEndianBinaryWriter _writer;

        private State _state;

        private int _appInfoLength;

        private int _sortInfoLength;

        private uint _dataOffset;

        private ushort _recordCount;

        private ushort _writtenEntryCount;

        private ushort _writtenDataCount;

        /// <summary>
        /// Initializes a new <see cref="PdbWriter"/>.
        /// </summary>
        /// <param name="stream">The destination stream.</param>
        /// <param name="leaveOpen">Controls whether the stream remains open after the writer is
        ///     disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="stream"/> does not support writing.
        ///     </exception>
        public PdbWriter(Stream stream, bool leaveOpen = false)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite)
                throw new ArgumentException("Stream must support writing.", nameof(stream));

            _stream = stream;
            _leaveOpen = leaveOpen;
            _writer = new BigEndianBinaryWriter(stream, Encoding.Default, leaveOpen: true);
        }

        /// <summary>
        /// Releases managed resources used by the instance.
        /// </summary>
        public void Dispose()
        {
            _writer.Dispose();

            if (!_leaveOpen)
                _stream.Dispose();
        }

        /// <summary>
        /// Writes the PDB header.
        /// </summary>
        /// <param name="name">The 32-byte name of the database.  The name must be NUL-terminated.
        ///     </param>
        /// <param name="attributes">The attributes of the database.</param>
        /// <param name="version">The application-specific version number of the database.</param>
        /// <param name="creationTime">The creation date/time of the database.</param>
        /// <param name="modificationTime">The date/time of the most recent modification of the
        ///     database.</param>
        /// <param name="backupTime">The date/time of the most recent backup of the database.
        ///     </param>
        /// <param name="modificationNumber">The modification number of the database.</param>
        /// <param name="appInfoLength">The length of the application-specific application info
        ///     block if one will be written; otherwise, <see langword="null"/>.</param>
        /// <param name="sortInfoLength">The length of the application-specific sort info block if
        ///     one will be written; otherwise, <see langword="null"/>.</param>
        /// <param name="typeId">The database type identifier.</param>
        /// <param name="creatorId">The database creator identifier.</param>
        /// <param name="uniqueIdSeed">A value used internally by the OS to generate unique
        ///     identifiers for records on the device when the database is loaded into the device.
        ///     </param>
        /// <param name="recordCount">The number of records in the database.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The length of <paramref name="name"/> is not 32.
        ///     </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="appInfoLength"/> is less
        ///     than 0.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="sortInfoLength"/> is less
        ///     than 0.</exception>
        /// <exception cref="NotSupportedException"><paramref name="attributes"/> has the
        ///     <see cref="PdbAttributes.ResourceDatabase"/> flag set.</exception>
        /// <exception cref="InvalidOperationException">The writer is not in a stat that allows
        ///     writing the header.</exception>
        /// <exception cref="InvalidOperationException">The position of the stream is not 0.
        ///     </exception>
        /// <exception cref="OverflowException">The PDB file is too large.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        // ExceptionAdjustment: P:System.IO.Stream.Position get -T:System.NotSupportedException
        public void WriteHeader(
            byte[] name,
            PdbAttributes attributes,
            ushort version,
            uint creationTime,
            uint modificationTime,
            uint backupTime,
            uint modificationNumber,
            int? appInfoLength,
            int? sortInfoLength,
            uint typeId,
            uint creatorId,
            uint uniqueIdSeed,
            ushort recordCount)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length != PdbFile.NameLength)
                throw new ArgumentException("Invalid length.", nameof(name));
            if ((attributes & PdbAttributes.ResourceDatabase) != 0)
                throw new NotSupportedException("Resource database is not supported.");
            if (appInfoLength < 0)
                throw new ArgumentOutOfRangeException(nameof(appInfoLength));
            if (sortInfoLength < 0)
                throw new ArgumentOutOfRangeException(nameof(sortInfoLength));

            if (_state != State.Initial)
                throw new InvalidOperationException("The writer is not in a state that allows writing the header.");
            if (_stream.CanSeek && _stream.Position != 0)
                throw new InvalidOperationException("Stream must be positioned at 0.");

            long offset = _headerLength;

            offset += _recordListHeaderLength + _recordEntryLength * recordCount + _recordListFooterLength;

            uint appInfoOffset;
            if (appInfoLength is int appInfoLengthValue)
            {
                _appInfoLength = appInfoLengthValue;

                appInfoOffset = (uint)offset;
                offset += (uint)appInfoLengthValue;

                if (offset > uint.MaxValue)
                    throw new OverflowException("File is too large.");
            }
            else
            {
                _appInfoLength = -1;

                appInfoOffset = 0;
            }

            uint sortInfoOffset;
            if (sortInfoLength is int sortInfoLengthValue)
            {
                _sortInfoLength = sortInfoLengthValue;

                sortInfoOffset = (uint)offset;
                offset += (uint)sortInfoLengthValue;

                if (offset > uint.MaxValue)
                    throw new OverflowException("File is too large.");
            }
            else
            {
                _sortInfoLength = -1;

                sortInfoOffset = 0;
            }

            _dataOffset = (uint)offset;

            try
            {
                // Header.
                _writer.Write(name, 0, name.Length);
                _writer.Write((ushort)attributes);
                _writer.Write(version);
                _writer.Write(creationTime);
                _writer.Write(modificationTime);
                _writer.Write(backupTime);
                _writer.Write(modificationNumber);
                _writer.Write(appInfoOffset);
                _writer.Write(sortInfoOffset);
                _writer.Write(typeId);
                _writer.Write(creatorId);
                _writer.Write(uniqueIdSeed);

                // Record list header.
                _writer.Write(0u);
                _writer.Write(recordCount);

                _recordCount = recordCount;

                if (_recordCount == 0)
                {
                    _writer.Write((ushort)0);
                    _writer.Flush();
                }

                if (_appInfoLength >= 0)
                    _state = _recordCount > 0 ? State.EntryOrAppInfo : State.AppInfo;
                else if (_sortInfoLength >= 0)
                    _state = _recordCount > 0 ? State.EntryOrSortInfo : State.SortInfo;
                else if (_recordCount > 0)
                    _state = State.EntryOrData;
                else
                    _state = State.Final;
            }
            catch
            {
                _state = State.Error;
                throw;
            }
        }

        /// <summary>
        /// Writes a record entry.  Writing the record entries normally follows writing the header,
        /// but can be done after writing the record data if the stream being written supports
        /// seeking.
        /// </summary>
        /// <param name="dataLength">The length of the record data.  This must be equal to the
        ///     number of bytes of data actually written for the record.</param>
        /// <param name="attributes">The attributes of the record.</param>
        /// <param name="category">The category of the record.</param>
        /// <param name="uniqueId">A 24-bit unique ID for the record.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="attributes"/> has an
        ///     invalid flag.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="category"/> is greater
        ///     than 0x0F.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="uniqueId"/> is greater
        ///     than 0x00FFFFFF.</exception>
        /// <exception cref="InvalidOperationException">The writer is not in a state that allows
        ///     writing a record entry.</exception>
        /// <exception cref="OverflowException">The PDB file is too large.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        public void WriteRecordEntry(
            uint dataLength,
            PdbRecordAttributes attributes,
            byte category,
            uint uniqueId)
        {
            const byte attributesMask = 0xF0;
            const byte categoryMask = 0x0F;
            const uint uniqueIdMask = 0x00FFFFFF;

            if (((byte)attributes & ~attributesMask) != 0)
                throw new ArgumentOutOfRangeException(nameof(attributes));
            if ((category & ~categoryMask) != 0)
                throw new ArgumentOutOfRangeException(nameof(category));
            if ((uniqueId & ~uniqueIdMask) != 0)
                throw new ArgumentOutOfRangeException(nameof(uniqueId));

            switch (_state)
            {
                case State.EntryOrAppInfo:
                case State.EntryOrSortInfo:
                case State.EntryOrData:
                case State.Entry:
                    break;
                default:
                    throw new InvalidOperationException("The writer is not in a state that allows writing a record entry.");
            }

            long nextDataOffset = (long)_dataOffset + dataLength;
            if (nextDataOffset > uint.MaxValue)
                throw new OverflowException("File is too large.");

            try
            {
                _writer.Write(_dataOffset);
                uint temp = (((uint)attributes | category) << 24) | uniqueId;
                _writer.Write(temp);

                _dataOffset = (uint)nextDataOffset;
                _writtenEntryCount += 1;

                if (_writtenEntryCount < _recordCount)
                {
                    _state = State.Entry;
                }
                else
                {
                    // Record list footer.
                    _writer.Write((ushort)0);
                    _writer.Flush();

                    if (_appInfoLength >= 0)
                    {
                        _state = State.AppInfo;
                    }
                    else if (_sortInfoLength >= 0)
                    {
                        _state = State.SortInfo;
                    }    
                    else if (_writtenDataCount == 0)
                    {
                        Debug.Assert(_writtenEntryCount == _recordCount);

                        _state = State.Data;
                    }
                    else
                    {
                        _state = State.Final;
                    }
                }
            }
            catch
            {
                _state = State.Error;
                throw;
            }            
        }

        /// <summary>
        /// Writes the application-specific application info block.
        /// </summary>
        /// <param name="appInfo">The application info data.</param>
        /// <exception cref="InvalidOperationException">The stream does not support seeking and the
        ///     record entries have not been written.</exception>
        /// <exception cref="InvalidOperationException">The writer is not in a state that allows
        ///     writing the application info.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        // ExceptionAdjustment: P:System.IO.Stream.Position -T:System.NotSupportedException
        // ExceptionAdjustment: M:System.IO.Stream.Write(System.Byte[],System.Int32,System.Int32) -T:System.NotSupportedException
        public void WriteAppInfo(byte[] appInfo)
        {
            switch (_state)
            {
                case State.EntryOrAppInfo:
                    if (!_stream.CanSeek)
                        throw new InvalidOperationException("The stream must support seeking to defer writing the record entries.");

                    Debug.Assert(_stream.Position == _headerLength + _recordListHeaderLength);
                    _stream.Position = _headerLength + _recordListHeaderLength + _recordEntryLength * _recordCount + _recordListFooterLength;
                    break;
                case State.AppInfo:
                    break;
                default:
                    throw new InvalidOperationException("The writer is not in a state that allows writing the application info.");
            }

            if (appInfo.Length != _appInfoLength)
                throw new InvalidOperationException("The length does not match the length specified when writing the header.");

            try
            {
                _stream.Write(appInfo, 0, appInfo.Length);

                if (_sortInfoLength >= 0)
                    _state = State.SortInfo;
                else if (_recordCount > 0)
                    _state = State.Data;
                else
                    _state = State.Final;
            }
            catch
            {
                _state = State.Error;
                throw;
            }
        }

        /// <summary>
        /// Writes the application-specific sort info block.
        /// </summary>
        /// <param name="sortInfo">The sort info data.</param>
        /// <exception cref="InvalidOperationException">The stream does not support seeking and the
        ///     record entries have not been written.</exception>
        /// <exception cref="InvalidOperationException">The writer is not in a state that allows
        ///     writing the sort info.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        // ExceptionAdjustment: P:System.IO.Stream.Position -T:System.NotSupportedException
        // ExceptionAdjustment: M:System.IO.Stream.Write(System.Byte[],System.Int32,System.Int32) -T:System.NotSupportedException
        public void WriteSortInfo(byte[] sortInfo)
        {
            switch (_state)
            {
                case State.EntryOrSortInfo:
                    if (!_stream.CanSeek)
                        throw new InvalidOperationException("The stream must support seeking to defer writing the record entries.");

                    Debug.Assert(_stream.Position == _headerLength + _recordListHeaderLength);
                    _stream.Position = _headerLength + _recordListHeaderLength + _recordEntryLength * _recordCount + _recordListFooterLength;
                    break;
                case State.SortInfo:
                    break;
                default:
                    throw new InvalidOperationException("The writer is not in a state that allows writing the sort info.");
            }

            if (sortInfo.Length != _sortInfoLength)
                throw new InvalidOperationException("The length does not match the length specified when writing the header.");

            try
            {
                _stream.Write(sortInfo, 0, sortInfo.Length);

                if (_recordCount > 0)
                    _state = State.Data;
                else
                    _state = State.Final;
            }
            catch
            {
                _state = State.Error;
                throw;
            }
        }

        /// <summary>
        /// Writes data of a record.
        /// </summary>
        /// <param name="data">The data to write.</param>
        /// <exception cref="InvalidOperationException">The stream does not support seeking and the
        ///     record entries have not been written.</exception>
        /// <exception cref="InvalidOperationException">The writer is not in a state that allows
        ///     writing the record data.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        // ExceptionAdjustment: M:System.IO.Stream.Write(System.Byte[],System.Int32,System.Int32) -T:System.NotSupportedException
        public void WriteRecordData(byte[] data)
        {
            ValidateWriteRecordData();

            try
            {
                _stream.Write(data, 0, data.Length);

                UpdateStateAfterWriteRecordData();
            }
            catch
            {
                _state = State.Error;
                throw;
            }
        }

        /// <summary>
        /// Writes data of a record.
        /// </summary>
        /// <param name="dataSource">The stream containing the data to write.</param>
        /// <returns>The number of bytes of data that were written.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dataSource"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="dataSource"/> is does not support
        ///     reading.</exception>
        /// <exception cref="InvalidOperationException">The stream does not support seeking and the
        ///     record entries have not been written.</exception>
        /// <exception cref="InvalidOperationException">The writer is not in a state that allows
        ///     writing the record data.</exception>
        /// <exception cref="OverflowException">The PDB file is too large.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        // ExceptionAdjustment: M:System.IO.Stream.Read(System.Byte[],System.Int32,System.Int32) -T:System.NotSupportedException
        // ExceptionAdjustment: M:System.IO.Stream.Write(System.Byte[],System.Int32,System.Int32) -T:System.NotSupportedException
        public uint WriteRecordData(Stream dataSource)
        {
            if (dataSource is null)
                throw new ArgumentNullException(nameof(dataSource));
            if (!dataSource.CanRead)
                throw new ArgumentException("Stream must support reading.", nameof(dataSource));

            ValidateWriteRecordData();

            long length = 0;

            try
            {
                byte[] buffer = new byte[16384];
                for (; ; )
                {
                    int readCount = dataSource.Read(buffer, 0, buffer.Length);
                    if (readCount == 0)
                        break;

                    _stream.Write(buffer, 0, readCount);

                    length += readCount;

                    if (length > uint.MaxValue)
                        throw new OverflowException("File is too large.");
                }

                UpdateStateAfterWriteRecordData();
            }
            catch
            {
                _state = State.Error;
                throw;
            }

            return (uint)length;
        }

        // ExceptionAdjustment: P:System.IO.Stream.Position -T:System.NotSupportedException
        /// <exception cref="InvalidOperationException">The stream does not support seeking and the
        ///     record entries have not been written.</exception>
        /// <exception cref="InvalidOperationException">The writer is not in a state that allows
        ///     writing the record data.</exception>
        /// <exception cref="IOException"/>
        private void ValidateWriteRecordData()
        {
            switch (_state)
            {
                case State.EntryOrData:
                    if (!_stream.CanSeek)
                        throw new InvalidOperationException("The stream must support seeking to defer writing the record entries.");

                    Debug.Assert(_stream.Position == _headerLength + _recordListHeaderLength);
                    _stream.Position = _headerLength + _recordListHeaderLength + _recordEntryLength * _recordCount + _recordListFooterLength;
                    break;
                case State.Data:
                    break;
                default:
                    throw new InvalidOperationException("The writer is not in a state that allows writing record data.");
            }
        }

        // ExceptionAdjustment: P:System.IO.Stream.Position set -T:System.NotSupportedException
        /// <exception cref="IOException"/>
        private void UpdateStateAfterWriteRecordData()
        {
            _writtenDataCount += 1;

            if (_writtenDataCount < _recordCount)
            {
                _state = State.Data;
            }
            else if (_writtenEntryCount == 0)
            {
                _stream.Position = _headerLength + _recordListHeaderLength;

                _state = State.Entry;
            }
            else
            {
                Debug.Assert(_writtenEntryCount == _recordCount);

                _state = State.Final;
            }
        }

        private enum State
        {
            Initial,
            EntryOrAppInfo,
            EntryOrSortInfo,
            EntryOrData,
            Entry,
            AppInfo,
            SortInfo,
            Data,
            Final,
            Error,
        }
    }
}
