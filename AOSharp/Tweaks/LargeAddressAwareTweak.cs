using System;
using System.Collections.Generic;
using System.IO;

namespace AOSharp.Tweaks
{
    public class LargeAddressAwareTweak
    {
        private enum PreflightCheckResult
        {
            Passed,
            Skipped,
            AnarchyIsRunning,
            AnarchyOnlineIsRunning,
            IsNewEngine,
            CannotWriteAnarchy,
            CannotWriteAnarchyOnline,
            PeParsingFailure,
            PatternSearchFailure
        }

        private string _integrityCheckFind    = "E8 ?? ?? ?? ?? 84 C0 75 ?? 39 1D ?? ?? ?? ?? 75 ?? 8B CF";
        private string _integrityCheckReplace = "90 90 90 90 90 90 90 EB ?? 39 1D ?? ?? ?? ?? ?? ?? ?? ??";
        private string _installPath;
        private string _anarchyExePath;
        private string _anarchyOnlineExePath;
        private string _anarchyVersion;
        private string _anarchyExeBackupPath;
        private string _anarchyOnlineExeBackupPath;

        private Dictionary<PreflightCheckResult, string> _errorMessages = new Dictionary<PreflightCheckResult, string>
        {
            { PreflightCheckResult.Passed, "Preflight checks passed." },
            { PreflightCheckResult.IsNewEngine, "The new engine is not supported, and it already supports large address aware." },
            { PreflightCheckResult.CannotWriteAnarchy, "Unable to open Anarchy.exe for writing. You must close the game launcher." },
            { PreflightCheckResult.CannotWriteAnarchyOnline, "Unable to open AnarchyOnline.exe for writing. You must close the game." },
            { PreflightCheckResult.PatternSearchFailure, "Unable to locate integrity check address for patching." }
        };

        public LargeAddressAwareTweak(string installPath)
        {
            _installPath = installPath;
            _anarchyExePath = Path.Combine(installPath, "Anarchy.exe");
            _anarchyOnlineExePath = Path.Combine(installPath, "AnarchyOnline.exe");
            _anarchyVersion = File.ReadAllText(Path.Combine(installPath, "version.id")).Trim();
            _anarchyExeBackupPath = $"{_anarchyExePath}_{_anarchyVersion}.bak";
            _anarchyOnlineExeBackupPath = $"{_anarchyOnlineExePath}_{_anarchyVersion}.bak";
        }

        public void Run()
        {
            PreflightCheckResult integrityCheckPreflightCheckResult = RunIntegrityCheckPreflightChecks();
            PreflightCheckResult largeAddressAwarePreflightCheckResult = RunLargeAddressAwarePreflightChecks();

            // all checks must pass before beginning any patching
            if (integrityCheckPreflightCheckResult != PreflightCheckResult.Passed)
                throw new Exception($"Integrity check preflight check failed: {_errorMessages[integrityCheckPreflightCheckResult]}");

            if (largeAddressAwarePreflightCheckResult != PreflightCheckResult.Passed)
                throw new Exception($"Large address aware preflight check failed: {_errorMessages[largeAddressAwarePreflightCheckResult]}");

            BackupFiles();
            PatchIntegrityCheck();
            PatchLargeAddressAware();
        }

        private PreflightCheckResult RunIntegrityCheckPreflightChecks()
        {
            if (IsNewEngine())
                return PreflightCheckResult.IsNewEngine;

            if (!CanOpenFileForWriting(_anarchyExePath))
                return PreflightCheckResult.CannotWriteAnarchy;

            // try to locate a patched or unpatched integrity check address
            var unpatchedReplacer = new BinaryPatternReplacer(_integrityCheckFind, _integrityCheckReplace);
            var unpatchedMatches = unpatchedReplacer.FindAndReplace(_anarchyExePath, performReplace: false);

            var patchedReplacer = new BinaryPatternReplacer(_integrityCheckReplace, _integrityCheckReplace);
            var patchedMatches = patchedReplacer.FindAndReplace(_anarchyExePath, performReplace: false);

            // there should be either one patched result, or one unpatched result
            if (!((unpatchedMatches.Count == 1 && patchedMatches.Count == 0) || (unpatchedMatches.Count == 0 && patchedMatches.Count == 1)))
                return PreflightCheckResult.PatternSearchFailure;

            return PreflightCheckResult.Passed;
        }

        private void PatchIntegrityCheck()
        {
            var replacer = new BinaryPatternReplacer(_integrityCheckFind, _integrityCheckReplace);
            replacer.FindAndReplace(_anarchyExePath);
        }

        private PreflightCheckResult RunLargeAddressAwarePreflightChecks()
        {
            if (IsNewEngine())
                return PreflightCheckResult.IsNewEngine;

            if (!CanOpenFileForWriting(_anarchyOnlineExePath))
                return PreflightCheckResult.CannotWriteAnarchyOnline;

            return PreflightCheckResult.Passed;
        }       
        private void PatchLargeAddressAware()
        {
            const int IMAGE_FILE_LARGE_ADDRESS_AWARE = 0x20;
            const ushort MZ_SIGNATURE = 0x5A4D;
            const uint PE_SIGNATURE = 0x4550;
            const int PE_HEADER_OFFSET_LOCATION = 0x3C;
            const int CHARACTERISTICS_OFFSET_FROM_PE_HEADER = 0x16;

            using (var fs = new FileStream(_anarchyOnlineExePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            using (var br = new BinaryReader(fs))
            using (var bw = new BinaryWriter(fs))
            {
                // Validate DOS header (MZ signature)
                if (br.ReadUInt16() != MZ_SIGNATURE)
                {
                    throw new InvalidOperationException("Invalid executable: Missing MZ header");
                }

                // Read PE header offset from DOS header
                fs.Position = PE_HEADER_OFFSET_LOCATION;
                int peHeaderOffset = br.ReadInt32();

                // Validate PE header signature
                fs.Position = peHeaderOffset;
                if (br.ReadUInt32() != PE_SIGNATURE)
                {
                    throw new InvalidOperationException("Invalid executable: Missing PE header");
                }

                // Navigate to Characteristics field in FILE_HEADER
                fs.Position = peHeaderOffset + CHARACTERISTICS_OFFSET_FROM_PE_HEADER;

                // Read current characteristics flags
                ushort characteristics = br.ReadUInt16();

                // Check if flag is already set
                if ((characteristics & IMAGE_FILE_LARGE_ADDRESS_AWARE) == IMAGE_FILE_LARGE_ADDRESS_AWARE)
                {
                    return; // Already set, nothing to do
                }

                // Set the flag
                characteristics |= IMAGE_FILE_LARGE_ADDRESS_AWARE;

                // Write updated flags back
                fs.Position = peHeaderOffset + CHARACTERISTICS_OFFSET_FROM_PE_HEADER;
                bw.Write(characteristics);
                bw.Flush();
            }
        }

        private void BackupFiles()
        {
            File.Copy(_anarchyExePath, _anarchyExeBackupPath, false);
            File.Copy(_anarchyOnlineExePath, _anarchyOnlineExeBackupPath, false);
        }

        private bool IsNewEngine()
        {
            return File.Exists(Path.Combine(_installPath, "Cheetah.dll"));
        }

        private bool CanOpenFileForWriting(string path)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(path);
                using (FileStream stream = fileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    stream.Close();
                }
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }
    }
}
