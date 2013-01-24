using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meebey.SmartIrc4net;

namespace nullbot
{
    abstract class ClientModule
    {
        private bool enabled;
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }
        
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        protected IrcClient client;
        protected GlobalStorage globalStorage;
        
        public ClientModule(string name)
        {
            this.name = name;
            this.enabled = true;
            this.client = Client.getInstance();
            this.globalStorage = GlobalStorage.getInstance();
        }
    }
}
