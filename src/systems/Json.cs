using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace MikeyXPCreamy.BioRand6
{
    public static class Json
    {
        public static T Read<T>(string path)
        {
            using (FileStream file = File.OpenRead(path))
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(file);
        }

        public static void Write<T>(string path, T value)
        {
            string full = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            string temp = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(file, new UTF8Encoding(false), false, true))
                    {
                        new DataContractJsonSerializer(typeof(T)).WriteObject(writer, value);
                        writer.Flush();
                    }
                    file.Flush(true);
                }
                DataFiles.MoveIntoPlace(temp, full);
            }
            finally
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
        }
    }
}
