using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meebey.SmartIrc4net;

namespace nullbot.Modules
{
    class KarmaModule : ClientModule
    {
        private const string activator = "!karma";
        private const int minute = 15 * 1000;
        private const int timeToDelay = minute * 1;

        public KarmaModule() : base("Karma") 
        {
            client.OnChannelMessage += client_OnChannelMessage;
            client.OnQueryMessage += client_OnQueryMessage;
        }

        void client_OnQueryMessage(object sender, IrcEventArgs e)
        {
            string message = e.Data.Message;
            string nick = e.Data.Nick;
            
            if (!globalStorage.IgnoredUsers.Contains(nick) && message.StartsWith(activator))
            {
                if (message.Equals(activator))
                {
                    log.VerboseMessage(nick + " sent karma activator, sending back how to write a request properly");
                    client.SendMessage(SendType.Message, nick, "Usage: " + activator + " [subject]");
                }
                else
                {
                    string karmaOf = message.Substring(activator.Length + 1); // this is the length plus a space
                    log.VerboseMessage(nick + " asked for karma of " + karmaOf);

                    if (globalStorage.karmaDatabase.ContainsKey(karmaOf))
                    {
                        int karma = globalStorage.karmaDatabase[karmaOf];
                        client.SendMessage(SendType.Message, nick, karmaOf + " has a karma of " + karma);
                        log.DebugMessage("karma is " + karma);
                    }
                    else
                    {
                        client.SendMessage(SendType.Message, nick, karmaOf + " has karma of 0.");
                        log.DebugMessage("karma is 0");
                    }
                }
            }
        }

        void client_OnChannelMessage(object sender, IrcEventArgs e)
        {
            string message = e.Data.Message;
            string nick = e.Data.Nick;
            string channel = e.Data.Channel;

            if (!globalStorage.IgnoredUsers.Contains(nick))
            {
                if (message.EndsWith("++"))
                {
                    string karmaOf = message.Substring(0, message.Length - 2);
                    if (globalStorage.karmaDatabase.ContainsKey(karmaOf))
                        globalStorage.karmaDatabase[karmaOf]++;
                    else
                        globalStorage.karmaDatabase[karmaOf] = 1;                        

                    log.VerboseMessage(nick + " => ++ => " + karmaOf);
                    log.DebugMessage("New karma is: " + globalStorage.karmaDatabase[karmaOf]);
                }
                else if (message.EndsWith("--"))
                {
                    string karmaOf = message.Substring(0, message.Length - 2);
                    if (globalStorage.karmaDatabase.ContainsKey(karmaOf))
                        globalStorage.karmaDatabase[karmaOf]--;
                    else
                        globalStorage.karmaDatabase[karmaOf] = -1;

                    log.VerboseMessage(nick + " => -- => " + karmaOf);
                    log.DebugMessage("New karma is: " + globalStorage.karmaDatabase[karmaOf]);
                }
                else if (message.StartsWith(activator))
                {
                    if (!message.Equals(activator))
                    {
                        string karmaOf = message.Substring(activator.Length + 1); // the activator plus a space
                        log.VerboseMessage(nick + " asking for karma of " + karmaOf + " in " + channel);
                        if (globalStorage.karmaDatabase.ContainsKey(karmaOf))
                        {
                            int karma = globalStorage.karmaDatabase[karmaOf];
                            client.SendMessage(SendType.Message, channel, karmaOf + " has a karma of " + karma);
                            log.DebugMessage("It has a karma of " + karma);
                        }
                        else
                        {
                            client.SendMessage(SendType.Message, channel, karmaOf + " has karma of 0.");
                            log.DebugMessage("It has a karma of 0");
                        }
                    }
                }
            }
        }

        
        
    }
}
