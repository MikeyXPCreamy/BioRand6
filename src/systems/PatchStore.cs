using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MikeyXPCreamy.BioRand6
{
    public sealed class PatchStore
    {
        private const string StoreFolder = DataLayout.BackupsFolder;
        private const string JournalName = "active.json";
        private const string LockName = "operation.lock";
        private const string OriginalsFolder = "Original files";
        private const string StatePrepared = "prepared";
        private const string StateApplying = "applying";
        private const string StateApplied = "applied";
        private const string StateRestoring = "restoring";

        private readonly string gameRoot;
        private readonly string dataRoot;
        private readonly string storeRoot;
        private readonly string journalPath;
        private readonly Func<bool> gameRunning;

        public PatchStore(string game, string dataDirectory, Func<bool> running)
        {
            gameRoot = Path.GetFullPath(game).TrimEnd(Path.DirectorySeparatorChar);
            dataRoot = Path.GetFullPath(dataDirectory);
            storeRoot = Path.Combine(dataRoot, StoreFolder);
            journalPath = Path.Combine(storeRoot, JournalName);
            gameRunning = running;
            DataFiles.NoLinks(journalPath);
        }

        public bool HasActive
        {
            get { return File.Exists(journalPath); }
        }

        public string ActiveSeedId
        {
            get
            {
                if (!HasActive)
                    return null;
                PatchJournal journal = Json.Read<PatchJournal>(journalPath);
                if (journal.Schema != PatchJournal.CurrentSchema || !SameGame(journal) || !Regex.IsMatch(journal.SeedId ?? "", @"\A[A-Za-z0-9_-]{1,64}\z"))
                    throw new InvalidDataException("Invalid active seed journal.");
                return journal.SeedId;
            }
        }

        public List<JournalFile> ActiveFiles()
        {
            return HasActive ? Json.Read<PatchJournal>(journalPath).Files : new List<JournalFile>();
        }

        public string ActiveBackendVersion
        {
            get { return HasActive ? Json.Read<PatchJournal>(journalPath).BackendVersion : null; }
        }

        private bool SameGame(PatchJournal journal)
        {
            return string.Equals(journal.GameRoot, gameRoot, StringComparison.OrdinalIgnoreCase);
        }

        private void CheckNotRunning()
        {
            if (gameRunning())
                throw new InvalidOperationException("Close Resident Evil 6 before changing its files.");
        }

        private FileStream OpenLock()
        {
            return new FileStream(Path.Combine(storeRoot, LockName), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        private static string OriginalName(string relativePath)
        {
            return OriginalsFolder + "/" + relativePath.Replace('\\', '/');
        }

        public string GamePath(string relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || relative.Contains(":"))
                throw new InvalidDataException("Patch path must be relative to the game.");
            string[] segments = relative.Replace('/', '\\').Split('\\');
            if (segments.Length < 2 || !string.Equals(segments[0], "nativePC", StringComparison.OrdinalIgnoreCase) || segments.Any(IsBadSegment))
                throw new InvalidDataException("Only regular files beneath nativePC can be patched.");
            string full = Path.GetFullPath(Path.Combine(gameRoot, string.Join("\\", segments)));
            if (!full.StartsWith(gameRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Patch path leaves the game folder.");
            DataFiles.NoLinks(full);
            return full;
        }

        private static bool IsBadSegment(string segment)
        {
            return segment == ".." || segment == "." || segment.Length == 0 || segment.EndsWith(".") || segment.EndsWith(" ") || segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0;
        }

        private string BackupPath(JournalFile file)
        {
            GamePath(file.RelativePath);
            bool inTransaction = Regex.IsMatch(file.BackupName ?? "", @"\Atx-[0-9a-f]{32}/[0-9]+\.original\z");
            if (!inTransaction && !string.Equals(file.BackupName, OriginalName(file.RelativePath), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Invalid backup location.");
            return DataFiles.Within(storeRoot, Path.Combine(storeRoot, file.BackupName.Replace('/', Path.DirectorySeparatorChar)));
        }

        public void VerifyApplied(string seedId, string backendVersion)
        {
            CheckNotRunning();
            DataFiles.NoLinks(storeRoot);
            if (!HasActive || ActiveSeedId != seedId)
                throw new InvalidDataException("The active game patch does not match this seed.");
            PatchJournal journal = Json.Read<PatchJournal>(journalPath);
            if (journal.State != StateApplied || journal.BackendVersion != backendVersion || journal.Files == null || journal.Files.Count == 0)
                throw new InvalidDataException("The active game patch needs recovery before launch.");

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (JournalFile file in journal.Files)
            {
                string target = GamePath(file.RelativePath);
                if (!seen.Add(target))
                    throw new InvalidDataException("Invalid patch recovery entry.");
                string backup = BackupPath(file);
                if (!File.Exists(target) || Hashes.FileHash(target) != file.PatchedSha256)
                    throw new IOException("Active game file changed: " + file.RelativePath);
                if (!File.Exists(backup) || Hashes.FileHash(backup) != file.OriginalSha256)
                    throw new IOException("Original archive backup verification failed.");
            }
        }

        public void Compact()
        {
            CheckNotRunning();
            DataFiles.NoLinks(dataRoot);
            Directory.CreateDirectory(storeRoot);
            using (OpenLock())
            {
                PatchJournal journal = HasActive ? MoveBackupsToOriginals() : null;
                CheckNotRunning();
                foreach (string transaction in Directory.GetDirectories(storeRoot, "tx-*"))
                {
                    if (Regex.IsMatch(Path.GetFileName(transaction), @"\Atx-[0-9a-f]{32}\z"))
                        DataFiles.Delete(dataRoot, transaction);
                }
                RemoveUnusedOriginals(journal);
            }
        }

        private PatchJournal MoveBackupsToOriginals()
        {
            PatchJournal journal = Json.Read<PatchJournal>(journalPath);
            VerifyApplied(journal.SeedId, journal.BackendVersion);
            var moved = new PatchJournal { GameRoot = gameRoot, SeedId = journal.SeedId, BackendVersion = journal.BackendVersion, State = StateApplied };
            foreach (JournalFile file in journal.Files)
            {
                string name = OriginalName(file.RelativePath);
                string target = DataFiles.Within(storeRoot, Path.Combine(storeRoot, name.Replace('/', Path.DirectorySeparatorChar)));
                DataFiles.CopyVerified(BackupPath(file), target, file.OriginalSha256);
                moved.Files.Add(new JournalFile { RelativePath = file.RelativePath, OriginalSha256 = file.OriginalSha256, PatchedSha256 = file.PatchedSha256, BackupName = name });
            }
            CheckNotRunning();
            Json.Write(journalPath, moved);
            VerifyApplied(moved.SeedId, moved.BackendVersion);
            return moved;
        }

        private void RemoveUnusedOriginals(PatchJournal journal)
        {
            string originals = Path.Combine(storeRoot, OriginalsFolder);
            if (!Directory.Exists(originals))
                return;
            DataFiles.CheckTree(dataRoot, originals);

            IEnumerable<string> needed = journal == null ? new string[0] : journal.Files.Select(BackupPath);
            var keep = new HashSet<string>(needed, StringComparer.OrdinalIgnoreCase);
            foreach (string file in Directory.GetFiles(originals, "*", SearchOption.AllDirectories))
            {
                if (!keep.Contains(file))
                    DataFiles.Delete(dataRoot, file);
            }
            foreach (string folder in Directory.GetDirectories(originals, "*", SearchOption.AllDirectories).OrderByDescending(path => path.Length))
            {
                if (!Directory.EnumerateFileSystemEntries(folder).Any())
                    DataFiles.Delete(dataRoot, folder);
            }
            if (!Directory.EnumerateFileSystemEntries(originals).Any())
                DataFiles.Delete(dataRoot, originals);
        }

        private static void ReplaceFrom(string source, string target)
        {
            string temp = target + ".biorand6-" + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (FileStream input = File.OpenRead(source))
                using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    input.CopyTo(output);
                    output.Flush(true);
                }
                File.Replace(temp, target, null);
            }
            finally
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
        }

        public void Apply(PatchPlan plan)
        {
            CheckNotRunning();
            DataFiles.NoLinks(storeRoot);
            Directory.CreateDirectory(storeRoot);
            using (OpenLock())
            {
                if (HasActive)
                    throw new InvalidOperationException("Restore the current seed before applying another one.");
                FilePatch[] files = CheckedFiles(plan);
                string transaction = "tx-" + Guid.NewGuid().ToString("N");
                string transactionRoot = Path.Combine(storeRoot, transaction);
                Directory.CreateDirectory(transactionRoot);
                PatchJournal journal = PrepareFiles(plan, files, transaction, transactionRoot);
                Json.Write(journalPath, journal);
                try
                {
                    journal.State = StateApplying;
                    Json.Write(journalPath, journal);
                    ReplaceGameFiles(files, journal, transactionRoot);
                    journal.State = StateApplied;
                    Json.Write(journalPath, journal);
                }
                catch (Exception failure)
                {
                    try
                    {
                        RestoreInternal();
                    }
                    catch (Exception recovery)
                    {
                        throw new IOException("Patch stopped. Backups were retained; restore is still required. " + recovery.Message, failure);
                    }
                    throw new IOException("Patch stopped and original files were restored. " + failure.Message, failure);
                }
            }
        }

        private FilePatch[] CheckedFiles(PatchPlan plan)
        {
            if (plan == null || plan.Files == null || plan.Files.Count == 0 || string.IsNullOrWhiteSpace(plan.BackendVersion) || string.IsNullOrWhiteSpace(plan.SeedId))
                throw new InvalidDataException("The backend did not produce a playable patch plan.");
            FilePatch[] files = plan.Files.OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase).ToArray();
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (FilePatch file in files)
            {
                string target = GamePath(file.RelativePath);
                if (!paths.Add(target))
                    throw new InvalidDataException("Duplicate patch file.");
                if (file.Content == null || !File.Exists(target) || Hashes.FileHash(target) != file.OriginalSha256)
                    throw new InvalidDataException("Original file does not match the supported build: " + file.RelativePath);
                if (Hashes.Bytes(file.Content) == file.OriginalSha256)
                    throw new InvalidDataException("Refusing a patch that makes no changes: " + file.RelativePath);
            }
            return files;
        }

        private PatchJournal PrepareFiles(PatchPlan plan, FilePatch[] files, string transaction, string transactionRoot)
        {
            var journal = new PatchJournal { GameRoot = gameRoot, SeedId = plan.SeedId, BackendVersion = plan.BackendVersion, State = StatePrepared };
            for (int i = 0; i < files.Length; i++)
            {
                FilePatch file = files[i];
                string backup = Path.Combine(transactionRoot, i + ".original");
                string staged = Path.Combine(transactionRoot, i + ".patched");
                File.Copy(GamePath(file.RelativePath), backup, false);
                if (Hashes.FileHash(backup) != file.OriginalSha256)
                    throw new IOException("Original changed during backup; no patch was applied.");
                using (var stream = new FileStream(staged, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(file.Content, 0, file.Content.Length);
                    stream.Flush(true);
                }
                journal.Files.Add(new JournalFile
                {
                    RelativePath = file.RelativePath,
                    OriginalSha256 = file.OriginalSha256,
                    PatchedSha256 = Hashes.FileHash(staged),
                    BackupName = transaction + "/" + i + ".original"
                });
            }
            return journal;
        }

        private void ReplaceGameFiles(FilePatch[] files, PatchJournal journal, string transactionRoot)
        {
            for (int i = 0; i < files.Length; i++)
            {
                CheckNotRunning();
                string target = GamePath(files[i].RelativePath);
                if (Hashes.FileHash(target) != journal.Files[i].OriginalSha256)
                    throw new IOException("Game file changed while preparing the seed.");
                ReplaceFrom(Path.Combine(transactionRoot, i + ".patched"), target);
                if (Hashes.FileHash(target) != journal.Files[i].PatchedSha256)
                    throw new IOException("Patched file failed verification.");
                GenerateProgress.File("Game file", target, files[i].RelativePath.Replace('/', '\\'));
            }
        }

        public void Restore()
        {
            CheckNotRunning();
            DataFiles.NoLinks(storeRoot);
            if (!HasActive)
                throw new InvalidOperationException("There is no active patch to restore.");
            using (OpenLock())
                RestoreInternal();
        }

        private void RestoreInternal()
        {
            CheckNotRunning();
            PatchJournal journal = Json.Read<PatchJournal>(journalPath);
            if (journal.Schema != PatchJournal.CurrentSchema || !SameGame(journal) || journal.Files == null || journal.Files.Count == 0)
                throw new InvalidDataException("Backup journal does not match this installation.");

            var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (JournalFile file in journal.Files)
            {
                string backup = BackupPath(file);
                string target = GamePath(file.RelativePath);
                if (!targets.Add(target))
                    throw new InvalidDataException("Duplicate journal target.");
                if (!File.Exists(backup) || Hashes.FileHash(backup) != file.OriginalSha256)
                    throw new IOException("Backup verification failed. No files were restored.");
                if (!File.Exists(target))
                    throw new IOException("A game file is missing. Restore stopped: " + file.RelativePath);
                string current = Hashes.FileHash(target);
                if (current != file.OriginalSha256 && current != file.PatchedSha256)
                    throw new IOException("A game file was changed by Steam or another mod. Restore stopped: " + file.RelativePath);
            }

            journal.State = StateRestoring;
            Json.Write(journalPath, journal);
            foreach (JournalFile file in journal.Files)
            {
                CheckNotRunning();
                string target = GamePath(file.RelativePath);
                string current = Hashes.FileHash(target);
                if (current != file.OriginalSha256 && current != file.PatchedSha256)
                    throw new IOException("A game file changed during restoration.");
                if (current != file.OriginalSha256)
                    ReplaceFrom(Path.Combine(storeRoot, file.BackupName), target);
                if (Hashes.FileHash(target) != file.OriginalSha256)
                    throw new IOException("Restored file failed verification.");
            }
            File.Delete(journalPath);
        }
    }
}
