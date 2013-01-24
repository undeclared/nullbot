using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nullbot.Modules
{
    class KarmaModule : ClientModule
    {
        public Dictionary<string, int> karmaDatabase;
        
        public KarmaModule()
            : base("Karma") 
        { 
        }
        
    }
}
