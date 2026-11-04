using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace MikeyXPCreamy.BioRand6
{
    public static class SteamGame
    {
        private const string AppId = "221040";
        private const string ManifestName = "appmanifest_" + AppId + ".acf";
        private const string ProcessName = "BH6";
        private const uint FullyInstalled = 4;

        private static string Value(string vdf, string key)
        {
            Match match = Regex.Match(vdf, "\"" + Regex.Escape(key) + "\"\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);
            return match.Success ? Unescape(match.Groups[1].Value) : "";
        }

        private static string Unescape(string value)
        {
            return value.Replace("\\\\", "\\");
        }

        private static void AddRegistryPath(List<string> paths, string key, string valueName)
        {
            try
            {
                string path = Convert.ToString(Registry.GetValue(key, valueName, null));
                if (!string.IsNullOrWhiteSpace(path))
                    paths.Add(path);
            }
            catch (SecurityException)
            {
            }
        }

        private static IEnumerable<string> Libraries()
        {
            var roots = new List<string>();
            AddRegistryPath(roots, @"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath");
            AddRegistryPath(roots, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath");
            AddRegistryPath(roots, @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath");
            roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"));

            var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string root in roots)
            {
                if (!Directory.Exists(root))
                    continue;
                libraries.Add(Path.GetFullPath(root));
                string vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(vdf))
                    continue;
                foreach (Match match in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s*\"([^\"]+)\""))
                    libraries.Add(Unescape(match.Groups[1].Value));
            }
            return libraries.OrderBy(library => library, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public static GameInfo Detect()
        {
            GameInfo notReady = null;
            foreach (string library in Libraries())
            {
                string manifest = Path.Combine(library, "steamapps", ManifestName);
                if (!File.Exists(manifest))
                    continue;
                string installDir = Value(File.ReadAllText(manifest), "installdir");
                if (string.IsNullOrEmpty(installDir) || installDir.IndexOfAny(new[] { '/', '\\', ':' }) >= 0 || installDir == "." || installDir == "..")
                    continue;
                GameInfo info = Inspect(Path.Combine(library, "steamapps", "common", installDir), false);
                if (info.Ready)
                    return info;
                notReady = info;
            }
            return notReady ?? new GameInfo { Message = "RE6 was not found in your Steam libraries. Choose its game folder." };
        }

        public static GameInfo Inspect(string root, bool fingerprint)
        {
            var info = new GameInfo { GameRoot = Path.GetFullPath(root), Message = "Game files are not ready." };
            ReadManifest(info);

            string exe = Path.Combine(info.GameRoot, "BH6.exe");
            string archives = Path.Combine(info.GameRoot, "nativePC", "arc", "DX9");
            if (Directory.Exists(archives))
                info.ArchiveCount = Directory.EnumerateFiles(archives, "*.arc", SearchOption.TopDirectoryOnly).Count();
            info.Ready = !info.Downloading && File.Exists(exe) && info.ArchiveCount > 0 && info.Manifest != null;

            if (info.Downloading)
                info.Message = "Steam is still downloading, updating, or verifying RE6.";
            else if (info.Manifest == null)
                info.Message = "Select the RE6 folder under your Steam library's steamapps\\common folder.";
            else if (!File.Exists(exe))
                info.Message = "BH6.exe is not available yet. Let Steam finish the installation.";
            else if (info.ArchiveCount == 0)
                info.Message = "The RE6 ARC files are not available yet.";
            else
                info.Message = "Installation found. Gameplay compatibility has not been verified yet.";

            if (fingerprint && info.Ready)
            {
                info.ExecutableSha256 = Hashes.FileHash(exe);
                info.ExecutableVersion = FileVersionInfo.GetVersionInfo(exe).FileVersion;
            }
            return info;
        }

        private static void ReadManifest(GameInfo info)
        {
            string folder = info.GameRoot.TrimEnd(Path.DirectorySeparatorChar);
            string common = Path.GetDirectoryName(folder);
            if (common == null || !string.Equals(Path.GetFileName(common), "common", StringComparison.OrdinalIgnoreCase))
                return;
            string manifest = Path.Combine(Path.GetDirectoryName(common), ManifestName);
            if (!File.Exists(manifest))
                return;

            string text = File.ReadAllText(manifest);
            if (Value(text, "appid") != AppId || !string.Equals(Value(text, "installdir"), Path.GetFileName(folder), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The Steam manifest does not match this RE6 folder.");
            info.Manifest = manifest;
            info.BuildId = Value(text, "buildid");
            uint.TryParse(Value(text, "StateFlags"), out info.StateFlags);
            long.TryParse(Value(text, "BytesDownloaded"), out info.Downloaded);
            long.TryParse(Value(text, "BytesToDownload"), out info.DownloadTotal);
            info.Downloading = info.StateFlags != FullyInstalled;
        }

        public static bool Running()
        {
            Process[] processes = Process.GetProcessesByName(ProcessName);
            bool found = processes.Length > 0;
            foreach (Process process in processes)
                process.Dispose();
            return found;
        }
    }
}
