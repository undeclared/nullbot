using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace nullbot
{
    [Serializable]
    class GlobalStorage
    {
        public List<string> IgnoredUsers;
        public Dictionary<string, int> lifetimePoints;
        public Dictionary<string, int> karmaDatabase;
        public List<string> quotes;

        private static GlobalStorage instance;

        public static GlobalStorage getInstance()
        {
            if (instance == null)
            {
                if (File.Exists("globals.xml"))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(GlobalStorage));
                    StreamReader file = new StreamReader(@"globals.xml");
                    instance = (GlobalStorage)serializer.Deserialize(file);
                }
                else
                {
                    instance = new GlobalStorage();
                    instance.IgnoredUsers = new List<string>();
                    instance.lifetimePoints = new Dictionary<string, int>();
                    instance.quotes = new List<string>();
                }
            }

            return instance;
        }

        public void Save()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(GlobalStorage));
            StreamWriter file = new StreamWriter(@"globals.xml");
            serializer.Serialize(file, this);
        }
    }
}
