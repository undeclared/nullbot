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
        private const string addQuoteActivator = "!addquoth";
        private const string findQuoteActivator = "!quoth";
        private Random random;

        public QuoteModule() : base("Quotes")
        {
            globalStorage = GlobalStorage.getInstance();
            random = new Random();
            client.OnChannelMessage += QuoteModule_OnChannelMessage;
        }

        void QuoteModule_OnChannelMessage(object sender, IrcEventArgs e)
        {
            string nick = e.Data.Nick;
            string channel = e.Data.Channel;
            string message = e.Data.Message;

            if (!globalStorage.IgnoredUsers.Contains(nick))
            {    
                if (message.ToLower().StartsWith(addQuoteActivator))
                {
                    log.VerboseMessage("Adding quote for " + nick);

                    string quote = message.Substring(addQuoteActivator.Length);
                    int quoteNumber = globalStorage.quotes.Count(); // the count is always equal to the next index if you think abiout it 
                    
                    globalStorage.quotes.Add(quote);
                    log.DebugMessage("Quote: " + quote);
                    
                    client.SendMessage(SendType.Message, "#cooking", "[ Quote #" + (quoteNumber+1) + "added! ]");
                    log.VerboseMessage("Quote #" + quoteNumber + " added.");
                }
                else if (message.ToLower().StartsWith(findQuoteActivator))
                {
                    if (message.Equals(findQuoteActivator))
                    {
                        if (globalStorage.quotes.Count > 1)
                        {
                            int quoteNum = random.Next(0, globalStorage.quotes.Count - 1);
                            string quote = globalStorage.quotes[quoteNum];
                            client.SendMessage(SendType.Message, channel, "[#" + (quoteNum + 1) + "] " + quote);

                            log.VerboseMessage("Sending quote #" + (quoteNum + 1) + " to " + channel + " on request of " + nick);
                            log.DebugMessage("Quote: " + quote);
                        }
                        else if (globalStorage.quotes.Count == 1)
                        {
                            client.SendMessage(SendType.Message, channel, "Only one quote! Here goes it..");
                            client.SendMessage(SendType.Message, channel, "[#1] " + globalStorage.quotes[0]);
                            log.VerboseMessage("Sending Quote #1 to " + channel + " on request of " + nick);
                        }
                        else if (globalStorage.quotes.Count == 0)
                        {
                            client.SendMessage(SendType.Message, channel, "No available quotes yet!");
                            log.VerboseMessage(nick + " asked for a quote on " + channel + " but there is none in DB.");
                        }
                    }
                    else
                    {
                        int quoteNum;
                        string probablyQuoteNum = message.Substring(findQuoteActivator.Length);
                        bool isNum = Int32.TryParse(probablyQuoteNum, out quoteNum);
                        
                        if (isNum)
                        {
                            log.VerboseMessage(nick + " asking for quote #" + quoteNum + " on " + channel);
                            int numQuotes = globalStorage.quotes.Count();
                            if (quoteNum >= numQuotes)
                            {
                                client.SendMessage(SendType.Message, channel, "Quote #" + quoteNum + " does not exist!");
                                log.VerboseMessage("Quote doesn't exist.");
                            }
                            else
                            {
                                string quote = globalStorage.quotes[quoteNum - 1]; // the actual quote number is -1, eg quote #1 is actually index 0
                                log.DebugMessage("Quote: " + quote);
                                client.SendMessage(SendType.Message, channel, "[#" + quoteNum + "] " + quote);
                            }
                        }

                    }
                }

            }
        }


    }
}
