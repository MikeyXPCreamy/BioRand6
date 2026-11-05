using System;
using System.IO;
using System.Text;

namespace MikeyXPCreamy.BioRand6
{
    public static class DataFiles
    {
        public static void NoLinks(string path)
        {
            for (string current = Path.GetFullPath(path); current != null; current = Path.GetDirectoryName(current))
            {
                if ((Directory.Exists(current) || File.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Linked files and directories are not supported: " + current);
            }
        }

        public static string Within(string root, string path)
        {
            string parent = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string full = Path.GetFullPath(path);
            if (!full.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Cleanup path leaves the randomizer data folder.");
            NoLinks(full);
            return full;
        }

        public static void CheckTree(string root, string path)
        {
            path = Within(root, path);
            if (!Directory.Exists(path))
                return;
            foreach (string child in Directory.GetFileSystemEntries(path))
                CheckTree(root, child);
        }

        public static void Delete(string root, string path)
        {
            CheckTree(root, path);
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            else if (File.Exists(path))
                File.Delete(path);
        }

        public static void MoveIntoPlace(string temp, string path)
        {
            if (File.Exists(path))
                File.Replace(temp, path, null);
            else
                File.Move(temp, path);
        }

        private static string TempPath(string path)
        {
            return path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        }

        public static void Write(string path, byte[] bytes)
        {
            NoLinks(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = TempPath(path);
            try
            {
                using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    file.Write(bytes, 0, bytes.Length);
                    file.Flush(true);
                }
                MoveIntoPlace(temp, path);
            }
            finally
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
        }

        public static void WriteText(string path, string text)
        {
            string pending = path + ".pending";
            File.WriteAllText(pending, text, new UTF8Encoding(false));
            MoveIntoPlace(pending, path);
        }

        public static void CopyVerified(string source, string target, string hash)
        {
            NoLinks(source);
            NoLinks(target);
            if (Hashes.FileHash(source) != hash)
                throw new IOException("A file changed while cleanup was being prepared.");
            if (File.Exists(target) && Hashes.FileHash(target) == hash)
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(target));
            string temp = TempPath(target);
            try
            {
                using (FileStream input = File.OpenRead(source))
                using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    input.CopyTo(output);
                    output.Flush(true);
                }
                if (Hashes.FileHash(temp) != hash || Hashes.FileHash(source) != hash)
                    throw new IOException("A copied file failed verification.");
                MoveIntoPlace(temp, target);
            }
            finally
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
        }
    }
}
