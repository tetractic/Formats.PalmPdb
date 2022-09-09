// Copyright 2021 Carl Reinke
//
// This file is part of a library that is licensed under the terms of the GNU
// Lesser General Public License Version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;

namespace Tetractic.Formats.PalmPdb
{
    /// <summary>
    /// The attributes of a Palm database record.
    /// </summary>
    /// <remarks>
    /// See "Record Attribute Constants" in the "Palm OS Programmer's API Reference" and
    /// "include/Core/System/DataMgr.h" in the Palm OS SDK.
    /// </remarks>
    [Flags]
    public enum PdbRecordAttributes : byte
    {
        /// <summary>
        /// No attributes.
        /// </summary>
        None,

        /// <summary>
        /// The record is password protected.
        /// </summary>
        Secret = 0x10,

        /// <summary>
        /// The record is in use by an application.
        /// </summary>
        Busy = 0x20,

        /// <summary>
        /// The record has been modified since last sync.
        /// </summary>
        Dirty = 0x40,

        /// <summary>
        /// The record will be deleted on next sync.
        /// </summary>
        Deleted = 0x80,
    }
}
