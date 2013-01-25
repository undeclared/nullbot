using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace nullbot
{
    [Serializable()]
    public class GlobalStorage
    {
        public List<string> IgnoredUsers;
        public SerializableDictionary<string, int> lifetimePoints;
        public SerializableDictionary<string, int> karmaDatabase;
        public List<string> quotes;

        private static XmlSerializer serializer;
        private StreamWriter file;

        private static GlobalStorage instance;

        public static GlobalStorage getInstance()
        {
            if (instance == null)
            {
                if (File.Exists("globals.xml"))
                {
                    serializer = new XmlSerializer(typeof(GlobalStorage));
                    StreamReader file = new StreamReader(@"globals.xml");
                    instance = (GlobalStorage)serializer.Deserialize(file);
                    file.Close();
                }
                else
                {
                    instance = new GlobalStorage();
                    instance.IgnoredUsers = new List<string>();
                    instance.lifetimePoints = new SerializableDictionary<string, int>();
                    instance.quotes = new List<string>();
                    instance.karmaDatabase = new SerializableDictionary<string, int>();
                }
            }

            return instance;
        }
        
        private GlobalStorage() { }

        public void Save()
        {
            Log.getInstance().DebugMessage("Saving global storage to globals.xml!");
            file = new StreamWriter(@"globals.xml");
            serializer.Serialize(file, instance);
            file.Close();
        }

        public void Close()
        {
            file.Close();
        }
    }
}
