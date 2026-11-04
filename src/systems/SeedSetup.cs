using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace MikeyXPCreamy.BioRand6
{
    // Field names and fingerprint text must never change.
    [DataContract]
    public sealed class SeedSetup
    {
        public const int AmmoNormal = 3;
        public const int AmmoMaxLevel = 5;
        public const int FirstGun = 0x101;
        public const int GunCount = 18;
        public const int MaxShare = 10;
        public const int MaxRoomsLevel = 7;
        public const int MaxPerRoom = 30;
        public const int AllGunsMask = (1 << GunCount) - 1;

        private const int DefaultRoomsLevel = 5;
        private const int DefaultPerRoom = 8;
        private const int MaxSeedLength = 128;
        private const int NewSeedBytes = 6;
        private const int FingerprintLength = 16;
        private const int SupportedSchema = 1;
        private const string SupportedAlgorithm = "re6-sha256-v1";
        private const string SupportedStatus = "configuration-only";

        [DataMember(Order = 0)] public int Schema = SupportedSchema;
        [DataMember(Order = 1)] public string Algorithm = SupportedAlgorithm;
        [DataMember(Order = 2)] public string Seed;
        [DataMember(Order = 3)] public bool Items = true;
        [DataMember(Order = 4)] public bool Weapons = true;
        [DataMember(Order = 5)] public bool ProtectProgression = true;
        [DataMember(Order = 6)] public string Status = SupportedStatus;
        [DataMember(Order = 7)] public bool CoOp;
        [DataMember(Order = 8)] public bool Enemies;
        [DataMember(Order = 9)] public bool Stalker;

        [DataMember(Order = 10)] public int Zombies;
        [DataMember(Order = 11)] public int Ustanaks;
        [DataMember(Order = 12)] public int Shriekers;
        [DataMember(Order = 13)] public int Whoppers;
        [DataMember(Order = 14)] public int Dogs;

        [DataMember(Order = 15)] public bool HasLayout;
        [DataMember(Order = 16)] public int EnemyRooms;
        [DataMember(Order = 17)] public int EnemyPerRoom;

        [DataMember(Order = 18)] public int Bloodshots;
        [DataMember(Order = 19)] public int Catacombs;

        [DataMember(Order = 20)] public bool CustomItems;
        [DataMember(Order = 21)] public int AmmoShare;
        [DataMember(Order = 22)] public int HerbShare;
        [DataMember(Order = 23)] public int SprayShare;
        [DataMember(Order = 24)] public int GrenadeShare;

        // 1 to 5. 0 means Normal.
        [DataMember(Order = 25)] public int AmmoAmount;

        // Bit i is gun FirstGun + i.
        [DataMember(Order = 26)] public int ExcludedGuns;

        // 0 is random.
        [DataMember(Order = 27)] public int StartWeapon1;
        [DataMember(Order = 28)] public int StartWeapon2;

        public int AmmoLevel
        {
            get { return AmmoAmount == 0 ? AmmoNormal : AmmoAmount; }
        }

        public int[] StartWeapons
        {
            get { return new[] { StartWeapon1, StartWeapon2 }.Where(weapon => weapon != 0).Distinct().ToArray(); }
        }

        public bool CustomWeapons
        {
            get { return Weapons && (EffectiveExcludedGuns != 0 || StartWeapon1 != 0 || StartWeapon2 != 0); }
        }

        public int EffectiveExcludedGuns
        {
            get
            {
                int mask = ExcludedGuns;
                foreach (int weapon in new[] { StartWeapon1, StartWeapon2 })
                {
                    if (IsGun(weapon))
                        mask &= ~(1 << (weapon - FirstGun));
                }
                return mask;
            }
        }

        public int RoomsLevel
        {
            get { return HasLayout ? EnemyRooms : DefaultRoomsLevel; }
        }

        public int PerRoom
        {
            get { return HasLayout ? EnemyPerRoom : DefaultPerRoom; }
        }

        public int ZombieWeight
        {
            get
            {
                bool noOtherKinds = UstanakWeight == 0 && Shriekers == 0 && Whoppers == 0 && Dogs == 0 && Bloodshots == 0 && Catacombs == 0;
                return Zombies == 0 && noOtherKinds ? MaxShare : Zombies;
            }
        }

        public int UstanakWeight
        {
            get { return Stalker ? 0 : Ustanaks; }
        }

        public bool ChangesGameFiles
        {
            get { return Items || Weapons || Enemies || Stalker; }
        }

        public static SeedSetup Create(string seed, bool items, bool weapons, bool coOp = false)
        {
            var result = new SeedSetup
            {
                Seed = (seed ?? "").Trim().Normalize(NormalizationForm.FormC),
                Items = items,
                Weapons = weapons,
                CoOp = coOp
            };
            result.Validate();
            return result;
        }

        public static SeedSetup Create(string seed, bool items, bool weapons, bool enemies, bool stalker)
        {
            SeedSetup result = Create(seed, items, weapons);
            result.Enemies = enemies;
            result.Stalker = stalker;
            return result;
        }

        public static string NewSeed()
        {
            var bytes = new byte[NewSeedBytes];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create())
                random.GetBytes(bytes);
            return "RE6-" + Hashes.Hex(bytes).ToUpperInvariant();
        }

        private static bool IsGun(int id)
        {
            return id >= FirstGun && id < FirstGun + GunCount;
        }

        public bool GunAllowed(int id)
        {
            return IsGun(id) && ((ExcludedGuns & (1 << (id - FirstGun))) == 0 || id == StartWeapon1 || id == StartWeapon2);
        }

        public void SetItemPool(int ammo, int herbs, int spray, int grenades)
        {
            CustomItems = true;
            AmmoShare = ammo;
            HerbShare = herbs;
            SprayShare = spray;
            GrenadeShare = grenades;
        }

        public void SetLayout(int roomsLevel, int perRoom)
        {
            HasLayout = true;
            EnemyRooms = roomsLevel;
            EnemyPerRoom = perRoom;
        }

        public void CopyOptionsTo(SeedSetup copy)
        {
            copy.Zombies = Zombies;
            copy.Ustanaks = Ustanaks;
            copy.Shriekers = Shriekers;
            copy.Whoppers = Whoppers;
            copy.Dogs = Dogs;
            copy.Bloodshots = Bloodshots;
            copy.Catacombs = Catacombs;
            if (HasLayout)
                copy.SetLayout(EnemyRooms, EnemyPerRoom);
            copy.CustomItems = CustomItems;
            copy.AmmoShare = AmmoShare;
            copy.HerbShare = HerbShare;
            copy.SprayShare = SprayShare;
            copy.GrenadeShare = GrenadeShare;
            copy.AmmoAmount = AmmoAmount;
            copy.ExcludedGuns = ExcludedGuns;
            copy.StartWeapon1 = StartWeapon1;
            copy.StartWeapon2 = StartWeapon2;
        }

        public void Validate()
        {
            if (Schema != SupportedSchema || Algorithm != SupportedAlgorithm)
                throw new InvalidDataException("This seed uses an unsupported format or algorithm.");
            if (string.IsNullOrWhiteSpace(Seed) || Seed.Length > MaxSeedLength || Seed.Any(char.IsControl))
                throw new InvalidDataException("Enter a seed of 1 to " + MaxSeedLength + " characters without line breaks.");
            if (Seed != Seed.Trim().Normalize(NormalizationForm.FormC))
                throw new InvalidDataException("The seed text is not in the supported canonical format.");
            if (!ProtectProgression || Status != SupportedStatus)
                throw new InvalidDataException("This build only supports seed setups with progression protection enabled.");

            if (new[] { Zombies, Ustanaks, Shriekers, Whoppers, Dogs, Bloodshots, Catacombs }.Any(OutsideShare))
                throw new InvalidDataException("The enemy mix must be 0 to " + MaxShare + " for each kind.");
            if (HasLayout && (EnemyRooms < 0 || EnemyRooms > MaxRoomsLevel || EnemyPerRoom < 1 || EnemyPerRoom > MaxPerRoom))
                throw new InvalidDataException("Rooms with enemies must be 0 to " + MaxRoomsLevel + " and enemies per room 1 to " + MaxPerRoom + ".");

            if (new[] { AmmoShare, HerbShare, SprayShare, GrenadeShare }.Any(OutsideShare))
                throw new InvalidDataException("The item pool must be 0 to " + MaxShare + " for each kind.");
            if (Items && CustomItems && AmmoShare + HerbShare + SprayShare + GrenadeShare == 0)
                throw new InvalidDataException("Choose at least one item kind for the custom item pool.");
            if (AmmoAmount < 0 || AmmoAmount > AmmoMaxLevel)
                throw new InvalidDataException("Ammo per pickup must be 1 to " + AmmoMaxLevel + ".");

            if ((ExcludedGuns & ~AllGunsMask) != 0)
                throw new InvalidDataException("The firearm list has an unknown weapon.");
            foreach (int weapon in new[] { StartWeapon1, StartWeapon2 })
            {
                if (weapon != 0 && !IsGun(weapon))
                    throw new InvalidDataException("A starting weapon must be Random or a firearm.");
            }
            if (Weapons && StartWeapon1 != 0 && StartWeapon1 == StartWeapon2)
                throw new InvalidDataException("Weapon 1 and Weapon 2 must be different firearms.");
            if (Weapons && !Enumerable.Range(FirstGun, GunCount).Any(GunAllowed))
                throw new InvalidDataException("Choose at least one firearm that can appear.");
        }

        private static bool OutsideShare(int share)
        {
            return share < 0 || share > MaxShare;
        }

        // Changing any line here changes existing seeds.
        public string Fingerprint()
        {
            Validate();
            string input = Algorithm + "\n" + Seed.Length + ":" + Seed + "\nitems=" + Items + "\nweapons=" + Weapons + "\nprotect=true";
            if (CoOp)
                input += "\ncoop=true";
            if (Enemies)
                input += "\nenemies=true";
            if (Stalker)
                input += "\nstalker=true";
            if (Enemies || Stalker)
                input += "\nenemy-rules=30";
            if (Enemies && UstanakWeight > 0)
                input += "\nenemy-mix=" + ZombieWeight + ":" + UstanakWeight;
            if (Enemies && Shriekers + Whoppers + Dogs > 0)
                input += "\nenemy-mix2=" + Shriekers + ":" + Whoppers + ":" + Dogs;
            if (Enemies && Bloodshots + Catacombs > 0)
                input += "\nenemy-mix3=" + Bloodshots + ":" + Catacombs;
            if (Enemies)
                input += "\nenemy-rooms=" + RoomsLevel + ":" + PerRoom;
            if (Items && CustomItems)
                input += "\nitem-pool=" + AmmoShare + ":" + HerbShare + ":" + SprayShare + ":" + GrenadeShare;
            if (Items && AmmoLevel != AmmoNormal)
                input += "\nammo-amount=" + AmmoLevel;
            if (Weapons && EffectiveExcludedGuns != 0)
                input += "\nguns-off=" + EffectiveExcludedGuns.ToString("x5");
            if (Weapons && (StartWeapon1 != 0 || StartWeapon2 != 0))
                input += "\nstart-guns=" + StartWeapon1.ToString("x3") + ":" + StartWeapon2.ToString("x3");
            return Hashes.Text(input).Substring(0, FingerprintLength).ToUpperInvariant();
        }
    }
}
