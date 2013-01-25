using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Meebey.SmartIrc4net;
using nullbot.Modules;

namespace nullbot
{
    class Client
    {
        private const string Nickname = "nullbot";
        private const string Server = "irc.servercentral.net";
        private const int Port = 6667;

        private static IrcClient instance;
        
        public static IrcClient getInstance()
        {
            if (instance == null)
                instance = new IrcClient();

            return instance;
        }

        public Client()
        {
            instance = new IrcClient();

            instance.OnConnected += clientInstance_OnConnected;
            instance.OnRegistered += clientInstance_OnRegistered;
            
            instance.Connect(Server, Port);

            Timer timer = new Timer(15 * 1000);
            timer.Elapsed += saveGlobalStorage;
            timer.Start();
        }

        void saveGlobalStorage(object sender, ElapsedEventArgs e)
        {
            GlobalStorage.getInstance().Save();
        }


        void clientInstance_OnRegistered(object sender, EventArgs e)
        {
            instance.RfcJoin("#cooking");
        }

        void clientInstance_OnConnected(object sender, EventArgs e)
        {
            instance.Login(Nickname, Nickname);
        }

        static void Main(string[] args)
        {
            new Client();
            new AcronymGame();
            new KarmaModule();
            new QuoteModule();

            instance.Listen();
        }
    }
}
