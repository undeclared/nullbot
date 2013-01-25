#define DEBUG
#define VERBOSE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nullbot
{
    // @TODO --> output to file
    class Log
    {
        private static Log instance;
        public static Log getInstance()
        {
            if (instance == null)
                instance = new Log();

            return instance;
        }

        private Log() { }

        public void DebugMessage(string message)
        {
          #if DEBUG
          Console.WriteLine("[DEBUG] " + message);
          #endif
        }

        public void VerboseMessage(string message)
        {
            #if VERBOSE
            Console.WriteLine("[VERBOSE] " + message);
            #endif
        }
    }
}
