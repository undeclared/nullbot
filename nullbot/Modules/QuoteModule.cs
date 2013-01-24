using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meebey.SmartIrc4net;

namespace nullbot.Modules
{
    class QuoteModule : ClientModule
    {
        private const string addQuoteActivator = "!addquote";
        private const string findQuoteActivator = "!quote";
        private Random random;

        public QuoteModule() : base("Quotes")
        {
            globalStorage = GlobalStorage.getInstance();
            random = new Random();
            client.OnChannelMessage += QuoteModule_OnChannelMessage;
        }

        void QuoteModule_OnChannelMessage(object sender, IrcEventArgs e)
        {
            if (!globalStorage.IgnoredUsers.Contains(e.Data.Nick))
            {
                string message = e.Data.Message;
                if (message.ToLower().StartsWith(addQuoteActivator))
                {
                    string quote = message.Substring(addQuoteActivator.Length);
                    int quoteNumber = globalStorage.quotes.Count(); // the count is always equal to the next index if you think abiout it 
                    globalStorage.quotes.Add(quote);
                    client.SendMessage(SendType.Message, "#cooking", "Quote added! #" + quoteNumber);
                    globalStorage.Save();
                }
                else if (message.ToLower().StartsWith(findQuoteActivator))
                {
                    if (message.Equals(findQuoteActivator))
                    {
                        if (globalStorage.quotes.Count > 1)
                        {
                            int quoteNum = random.Next(0, globalStorage.quotes.Count - 1);
                            string quote = globalStorage.quotes[quoteNum];
                            client.SendMessage(SendType.Message, e.Data.Channel, "[#" + (quoteNum + 1) + "] " + quote);
                        }
                        else if (globalStorage.quotes.Count == 1)
                        {
                            client.SendMessage(SendType.Message, e.Data.Channel, "Only one quote! Here goes it..");
                            client.SendMessage(SendType.Message, e.Data.Channel, "[#1] " + globalStorage.quotes[0]);
                        }
                        else if (globalStorage.quotes.Count == 0)
                        {
                            client.SendMessage(SendType.Message, e.Data.Channel, "No available quotes yet!");
                        }
                    }
                    else
                    {

                    }
                }

            }
        }


    }
}
