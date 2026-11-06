using System;
using System.IO;
using System.Linq;

namespace MikeyXPCreamy.BioRand6
{
    internal static class EnemyRoomList
    {
        // Room order is part of what a seed draws.
        public static EnemyRoom[] All()
        {
            return EnemyRoomsS0104.Rooms()
                .Concat(EnemyRoomsS0105.Rooms())
                .Concat(EnemyRoomsS0101.Rooms())
                .Concat(EnemyRoomsS0102.Rooms())
                .Concat(EnemyRoomsS0103.Rooms())
                .Concat(EnemyRoomsS0104Return.Rooms())
                .ToArray();
        }

        // Each spot is x, y, z and clearance.
        public static float[][] Spots(params float[] values)
        {
            if (values.Length == 0 || values.Length % 4 != 0)
                throw new InvalidDataException("A room's spots must each have x, y, z and clearance.");
            var spots = new float[values.Length / 4][];
            for (int i = 0; i < spots.Length; i++)
            {
                spots[i] = new float[4];
                Array.Copy(values, i * 4, spots[i], 0, 4);
            }
            return spots;
        }
    }
}