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
    /// The attributes of a Palm database.
    /// </summary>
    /// <remarks>
    /// See "Database Attribute Constants" in the "Palm OS Programmer's API Reference" and
    /// "include/Core/device/DataMgr.h" in the Palm OS SDK.
    /// </remarks>
    [Flags]
    public enum PdbAttributes : ushort
    {
        /// <summary>
        /// No attributes.
        /// </summary>
        None = 0,

        /// <summary>
        /// The database is a resource database.
        /// </summary>
        ResourceDatabase = 0x1,

        /// <summary>
        /// The database is a read-only database.
        /// </summary>
        ReadOnly = 0x2,

        /// <summary>
        /// The application info block of the database has been modified since the last sync.
        /// </summary>
        AppInfoDirty = 0x4,

        /// <summary>
        /// The database should be backed up to the desktop computer if no application-specific
        /// conduit is available.
        /// </summary>
        Backup = 0x8,

        /// <summary>
        /// The backup conduit can install a newer version of the database with a different name if
        /// the database is open.
        /// </summary>
        OkayToInstallNewer = 0x10,

        /// <summary>
        /// The device must be reset after the database is installed.
        /// </summary>
        ResetAfterInstall = 0x20,

        /// <summary>
        /// Disallows the database being copying by methods such as IR beaming.
        /// </summary>
        CopyPrevention = 0x40,

        /// <summary>
        /// The database is a file stream.
        /// </summary>
        Stream = 0x80,

        /// <summary>
        /// The database should be hidden from view.  For example, this attribute is set to hide
        /// some applications in the launcher's main view.  You can set it on record databases to
        /// have the launcher disregard the database's records when showing a count of records.
        /// </summary>
        Hidden = 0x100,

        /// <summary>
        /// The database is a data database that can be “launched” from the launcher by opening it
        /// with its application.
        /// </summary>
        LaunchableData = 0x200,

        /// <summary>
        /// The database will be deleted when closed or upon a device reset.
        /// </summary>
        Recyclable = 0x400,

        /// <summary>
        /// The database is copied with it application when its application is copied by methods
        /// such as IR beaming.  Resource and overlay databases are automatically copied with their
        /// application without having this attribute set.
        /// </summary>
        Bundle = 0x800,

        /// <summary>
        /// The database is open.
        /// </summary>
        Open = 0x8000,
    }
}
