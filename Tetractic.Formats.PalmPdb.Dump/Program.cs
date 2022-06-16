// Copyright 2021 Carl Reinke
//
// This file is part of a program that is licensed under the terms of GNU Lesser
// General Public License version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using System.Buffers.Binary;
using System.IO;
using Tetractic.CommandLine;

namespace Tetractic.Formats.PalmPdb.Dump
{
    internal static class Program
    {
        /// <exception cref="IOException"/>
        internal static int Main(string[] args)
        {
            var rootCommand = new RootCommand(GetExecutableName("PdbDump"));

            var pathParameter = rootCommand.AddParameter("<pdb>", "The path of a Palm PDB file.");

            var useMacEpochOption = rootCommand.AddOption('m', null, "Show Mac (rather than Unix) timestamps.");
            var dumpHexOption = rootCommand.AddOption('x', null, "Dump hex data to console.");
            var dumpFilesOption = rootCommand.AddOption('o', null, "Dump data to files.");

            rootCommand.HelpOption = rootCommand.AddOption('h', "help", "Shows a usages summary.");

            rootCommand.SetInvokeHandler(() =>
            {
                string path = pathParameter.Value;
                bool useMacEpoch = useMacEpochOption.Count > 0;
                bool dumpHex = dumpHexOption.Count > 0;
                bool dumpFiles = dumpFilesOption.Count > 0;

                return Dump(path, useMacEpoch, dumpHex, dumpFiles);
            });

            try
            {
                return rootCommand.Execute(args);
            }
            catch (InvalidCommandLineException ex)
            {
                Console.Error.WriteLine(ex.Message);
                CommandHelp.WriteHelpHint(ex.Command, Console.Error);
                return -1;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.Error.WriteLine(ex);
#else
                Console.Error.WriteLine(ex.Message);
#endif
                return -1;
            }
        }

        private static string GetExecutableName(string defaultName)
        {
            try
            {
                string[] commandLineArgs = Environment.GetCommandLineArgs();
                if (commandLineArgs.Length > 0)
                    return Path.GetFileNameWithoutExtension(commandLineArgs[0]);
            }
            catch
            {
                // Fall through.
            }

            return defaultName;
        }

        /// <exception cref="Exception"/>
        private static int Dump(string path, bool useMacEpoch, bool dumpHex, bool dumpFiles)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 0x10000))
            using (var pdb = new PdbFile(stream))
            {
                var epoch = new DateTime(useMacEpoch ? 1904 : 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var creationTime = epoch + new TimeSpan(pdb.CreationTime * TimeSpan.TicksPerSecond);
                var modificationTime = epoch + new TimeSpan(pdb.ModificationTime * TimeSpan.TicksPerSecond);
                var backupTime = epoch + new TimeSpan(pdb.BackupTime * TimeSpan.TicksPerSecond);

                Console.WriteLine($"Name:");
                Hex.Dump(pdb.Name, indent: "  ");
                Console.WriteLine($"Attributes:         0x{(ushort)pdb.Attributes:X4} ({pdb.Attributes})");
                Console.WriteLine($"Version:            {pdb.Version}");
                Console.WriteLine($"CreationTime:       {pdb.CreationTime} ({creationTime})");
                Console.WriteLine($"ModificationTime:   {pdb.ModificationTime} ({modificationTime})");
                Console.WriteLine($"BackupTime:         {pdb.BackupTime} ({backupTime})");
                Console.WriteLine($"ModificationNumber: {pdb.ModificationNumber}");
                Console.WriteLine($"AppInfoOffset:      0x{pdb.OriginalAppInfoOffset:X8}");
                Console.WriteLine($"SortInfoOffset:     0x{pdb.OriginalSortInfoOffset:X8}");
                Console.WriteLine($"TypeId:             0x{pdb.TypeId:X8} ({GetPrintable(pdb.TypeId)})");
                Console.WriteLine($"CreatorId:          0x{pdb.CreatorId:X8} ({GetPrintable(pdb.CreatorId)})");
                Console.WriteLine($"UniqueIdSeed:       {pdb.UniqueIdSeed}");
                Console.WriteLine($"RecordCount:        {pdb.Records.Count}");
                Console.WriteLine();

                if (pdb.AppInfo != null)
                {
                    Console.WriteLine($"AppInfo: {pdb.AppInfo.Length} bytes");
                    if (dumpHex)
                        Hex.Dump(pdb.AppInfo, writeOffset: true);
                    Console.WriteLine();
                }

                if (pdb.SortInfo != null)
                {
                    Console.WriteLine($"SortInfo: {pdb.SortInfo.Length} bytes");
                    if (dumpHex)
                        Hex.Dump(pdb.SortInfo, writeOffset: true);
                    Console.WriteLine();
                }

                var records = pdb.Records;

                for (int i = 0; i < records.Count; i++)
                {
                    PdbRecord record = records[i];

                    Console.WriteLine($"Record {i}:");
                    Console.WriteLine($"DataOffset: 0x{record.OriginalDataOffset:X8}");
                    Console.WriteLine($"Attributes: 0x{(byte)record.Attributes >> 4:X1} ({record.Attributes})");
                    Console.WriteLine($"Category:   {record.Category}");
                    Console.WriteLine($"UniqueId:   {record.UniqueId}");
                    Console.WriteLine($"Data: {record.DataLength} bytes");
                    if (dumpHex)
                        using (var recordStream = record.OpenData(FileAccess.Read))
                            Hex.Dump(recordStream, writeOffset: true);
                    Console.WriteLine();
                }

                if (dumpFiles)
                {
                    if (pdb.AppInfo != null)
                        using (var outStream = new FileStream($"{path}-appInfo.bin", FileMode.Create, FileAccess.Write, FileShare.Read, 0x10000))
                            outStream.Write(pdb.AppInfo, 0, pdb.AppInfo.Length);

                    if (pdb.SortInfo != null)
                        using (var outStream = new FileStream($"{path}-sortInfo.bin", FileMode.Create, FileAccess.Write, FileShare.Read, 0x10000))
                            outStream.Write(pdb.SortInfo, 0, pdb.SortInfo.Length);

                    for (int i = 0; i < records.Count; i++)
                    {
                        PdbRecord record = records[i];

                        using (var source = record.OpenData(FileAccess.Read))
                        using (var outStream = new FileStream($"{path}-{i:D5}.bin", FileMode.Create, FileAccess.Write, FileShare.Read, 0x10000))
                            source.CopyTo(outStream);
                    }
                }
            }

            return 0;
        }

        private static string GetPrintable(uint id)
        {
            Span<byte> bytes = stackalloc byte[4];
            char[] chars = new char[4];

            BinaryPrimitives.WriteUInt32BigEndian(bytes, id);

            for (int i = 0; i < 4; ++i)
            {
                byte b = bytes[i];
                chars[i] = b < ' ' ? '.' : (char)b;
            }

            return new string(chars);
        }
    }
}
