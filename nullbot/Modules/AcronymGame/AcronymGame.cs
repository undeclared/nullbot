using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meebey.SmartIrc4net;
using System.Timers;

namespace nullbot.Modules
{
    class AcronymGame : ClientModule
    {
        private const byte MIN_LENGTH = 5;
        private const byte MAX_LENGTH = 8;
        private const byte GAME_LENGTH_MINUTES = 2;
        private const string LETTERS = "abcdefghijklmnorstuvwy";
        
        private bool active;
        private bool sentOneWarning;
        private IrcClient client;
        private Timer timer;
        private string currentAcronym;
        private Random random;
        private DateTime startTime;
        private Dictionary<string, AcronymProposal> proposedAcronyms;

        public AcronymGame() : base("Acronym Game")
        {
            active = false;
            sentOneWarning = false;
            client = Client.getInstance();
            random = new Random();
            timer = new Timer();
            proposedAcronyms = new Dictionary<string, AcronymProposal>();
            client.OnQueryMessage += client_OnQueryMessage;
            client.OnChannelMessage += client_OnChannelMessage;
        }

        void client_OnChannelMessage(object sender, IrcEventArgs e)
        {
            if (e.Data.Message == "!acronym" && 
                !Globals.getInstance().IgnoredUsers.Contains(e.Data.Nick) &&
                !active)
            {
                TimeSpan sinceLastGame = DateTime.Now.Subtract(startTime);
                
                if(sinceLastGame.TotalSeconds >= 60)
                    newGame();
                else if (!sentOneWarning)
                {
                    client.SendMessage(SendType.Message, "#cooking", "Cannot start game yet.  Please wait another " + (60 - sinceLastGame.TotalSeconds) + " second(s) and try again.");
                    sentOneWarning = true;
                }
            }
        }

        private void newGame()
        {
            generateNewAcronym();
            client.SendMessage(SendType.Message, "#cooking", "New acronym: " + currentAcronym + ". Message me with a proposed meaning (one per game). Results in " + GAME_LENGTH_MINUTES + " minute(s).");
            active = true;
            startTime = DateTime.Now;
            timer.Interval = GAME_LENGTH_MINUTES * 60 * 1000;
            timer.Elapsed += gameOver;
            timer.Start();
        }

        void gameOver(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
            active = false;
            sentOneWarning = false;
            if (proposedAcronyms.Keys.Count == 0)
            {
                client.SendMessage(SendType.Message, "#cooking", "Acronym: " + currentAcronym + ". Nobody gave any meanings.");
                return;
            }

            client.SendMessage(SendType.Message, "#cooking", "Acronym: " + currentAcronym + ". Time is up! Listing the proposed meanings.");

            foreach (KeyValuePair<string, AcronymProposal> acronymKvp in proposedAcronyms)
            {
                AcronymProposal acronymProposal = acronymKvp.Value;

                string finalString = "(" + acronymProposal.nickname + ") " + acronymProposal.acronym + " [" + acronymProposal.timeSpanString + "]";
                client.SendMessage(SendType.Message, "#cooking", finalString);
            }

            client.SendMessage(SendType.Message, "#cooking", "Listing over. Thanks for playing!");

            proposedAcronyms.Clear();
        }

        private void generateNewAcronym()
        {
            currentAcronym = String.Empty;
            
            int length = random.Next(AcronymGame.MIN_LENGTH, AcronymGame.MAX_LENGTH);
            int lengthOfLetters = LETTERS.Length;

            int randomLetterIndex = -1;
            char letter = ' ';
            for (int index = 0; index < length; index++)
            {
                randomLetterIndex = random.Next(0, lengthOfLetters - 1);
                
                if (LETTERS[randomLetterIndex] == letter)
                {
                    index--;
                    continue;
                }
                letter = LETTERS[randomLetterIndex];

                currentAcronym += letter;
            }
        }

        void client_OnQueryMessage(object sender, IrcEventArgs e)
        {
            char space = ' ';
            if (active)
            {
                string nick = e.Data.Nick;
                bool alreadyExists = proposedAcronyms.ContainsKey(nick);

                if (!Globals.getInstance().IgnoredUsers.Contains(nick))
                {
                    string message = e.Data.Message;
                    string[] words = message.Split(space);
                    int length = words.Length;
                    int expectedLength = currentAcronym.Length;

                    if (length != expectedLength)
                    {
                        client.SendMessage(SendType.Message, nick, "There is something wrong with the acronym you sent.  Expected " + expectedLength + " words.  You sent " + length + " word(s).");

                        if(alreadyExists)
                            client.SendMessage(SendType.Message, nick, "Old acronym not replaced.");

                        return;
                    }
                    else
                    {
                        int index = -1;
                        foreach(string word in words)
                        {
                            index++;

                            char firstLetter = Char.ToLower(word[0]);
                            char expectedLetter = currentAcronym[index];

                            if (firstLetter != expectedLetter)
                            {
                                client.SendMessage(SendType.Message, nick, "There is something wrong with the acronym you sent (mismatched letters). Please verify and re-send.");

                                if (alreadyExists)
                                    client.SendMessage(SendType.Message, nick, "Old acronym not replaced.");

                                return;
                            }
                        }
                    }

                    TimeSpan timeSpan = DateTime.Now.Subtract(startTime);
                    string timeString = timeSpan.Seconds.ToString() + " seconds";
                    
                    if (timeSpan.Minutes > 0)
                        timeString = timeSpan.Minutes.ToString() + " minute " + timeString;

                    AcronymProposal acronymProposal = new AcronymProposal();
                    acronymProposal.nickname = nick;
                    acronymProposal.timeSpanString = timeString;
                    acronymProposal.acronym = message;
                    acronymProposal.index = AcronymProposal.lastIndex++;
                    // No errors! time to add the string. Replaces if an old one already exists.
                    proposedAcronyms[nick] = acronymProposal;
                }
            }
        }
    }
}
