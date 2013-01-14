using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nullbot.Modules
{
    public struct AcronymProposal
    {
        public static int lastIndex = 1; // start at 1.

        public int index;
        public string nickname;
        public string acronym;
        public string timeSpanString;
    }
}
