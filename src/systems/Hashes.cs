using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MikeyXPCreamy.BioRand6
{
    public static class Hashes
    {
        public static string Bytes(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
                return Hex(sha.ComputeHash(bytes));
        }

        public static string Text(string value)
        {
            return Bytes(Encoding.UTF8.GetBytes(value));
        }

        public static string FileHash(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream file = File.OpenRead(path))
                return Hex(sha.ComputeHash(file));
        }

        public static string Hex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
