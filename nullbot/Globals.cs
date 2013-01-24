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
    class Globals
    {
        public List<string> IgnoredUsers;
        public Dictionary<string, int> lifetimePoints;

        private static Globals instance;

        public static Globals getInstance()
        {
            if (instance == null)
            {
                if (File.Exists("globals.xml"))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Globals));
                    StreamReader file = new StreamReader(@"globals.xml");
                    instance = (Globals)serializer.Deserialize(file);
                }
                else
                {
                    instance = new Globals();
                    instance.IgnoredUsers = new List<string>();
                    instance.lifetimePoints = new Dictionary<string, int>();
                }
            }

            return instance;
        }

        public void Save()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Globals));
            StreamWriter file = new StreamWriter(@"globals.xml");
            serializer.Serialize(file, this);
        }
    }
}
