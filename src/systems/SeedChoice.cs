using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MikeyXPCreamy.BioRand6
{
    public static class SeedChoice
    {
        public static int Pick(SeedSetup seed, string rule, string location, int count)
        {
            seed.Validate();
            if (count < 1)
                throw new ArgumentOutOfRangeException("count");
            if (string.IsNullOrEmpty(rule) || string.IsNullOrEmpty(location))
                throw new ArgumentException("A backend version and stable location ID are required.");

            ulong range = 1UL << 32;
            ulong bound = range - range % (uint)count;
            for (uint attempt = 0; ; attempt++)
            {
                string input = seed.Fingerprint() + "|" + rule.Length + ":" + rule + "|" + location.Length + ":" + location + "|" + attempt.ToString(CultureInfo.InvariantCulture);
                byte[] hash;
                using (SHA256 sha = SHA256.Create())
                    hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                uint value = (uint)hash[0] | ((uint)hash[1] << 8) | ((uint)hash[2] << 16) | ((uint)hash[3] << 24);
                if (value < bound)
                    return (int)(value % (uint)count);
            }
        }
    }
}
