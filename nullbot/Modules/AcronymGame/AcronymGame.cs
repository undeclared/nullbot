﻿using System;
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
        private const int MIN_LENGTH_DEFAULT = 5;
        private const int MAX_LENGTH_DEFAULT = 8;
        private const byte GAME_LENGTH_MINUTES = 2;
        private const byte VOTE_LENGTH_MINUTES = 1;
        private const string LETTERS = "abcdefghijklmnoprstuvwy";
        
        private bool gameTime;
        private bool votingTime;
        private IrcClient client;
        private Timer gameTimer;
        private Timer voteTimer;
        private string currentAcronym;
        private Random random;
        private DateTime startTime;
        private List<AcronymProposal> proposedAcronyms;
        private Dictionary<string, int> peopleWhoVotedAndForWho;
        private Dictionary<string, int> voteTotals;
        private int minLength;
        private int maxLength;

        public AcronymGame() : base("Acronym Game")
        {
            gameTime = false;
            votingTime = false; 
            client = Client.getInstance();
            random = new Random();
            gameTimer = new Timer(GAME_LENGTH_MINUTES * 60 * 1000);
            gameTimer.Elapsed += startVote;
            voteTimer = new Timer(VOTE_LENGTH_MINUTES * 60 * 1000);
            voteTimer.Elapsed += endGame;
            proposedAcronyms = new List<AcronymProposal>();
            peopleWhoVotedAndForWho = new Dictionary<string, int>();
            voteTotals = new Dictionary<string, int>();
            client.OnQueryMessage += client_OnQueryMessage;
            client.OnChannelMessage += client_OnChannelMessage;

            minLength = MIN_LENGTH_DEFAULT;
            maxLength = MAX_LENGTH_DEFAULT;
        }

        void client_OnChannelMessage(object sender, IrcEventArgs e)
        {
            Globals globals = Globals.getInstance();
            string message = e.Data.Message;
            string nick = e.Data.Nick;

            if (message.StartsWith("!acronym") &&
                !globals.IgnoredUsers.Contains(nick) &&
                !gameTime &&
                !votingTime)
            {
                TimeSpan sinceLastGame = DateTime.Now.Subtract(startTime);

                string[] commands = message.Split(' ');

                if (commands.Length == 2)
                {
                    bool isNum = Int32.TryParse(commands[1], out minLength);
                    
                    if (isNum)
                        maxLength = minLength;
                }
                else
                {
                    minLength = MIN_LENGTH_DEFAULT;
                    maxLength = MAX_LENGTH_DEFAULT;
                }

                newGame();
            }
        }

        private void newGame()
        {
            generateNewAcronym();
            client.SendMessage(SendType.Message, "#cooking", "New acronym: " + currentAcronym + ". Message me with a proposed meaning (one per game). Results in " + GAME_LENGTH_MINUTES + " minute(s).");
            Console.WriteLine("Generated new acronym: " + currentAcronym);
            gameTime = true;
            startTime = DateTime.Now;
            gameTimer.Start();
        }

        void startVote(object sender, ElapsedEventArgs e)
        {
            gameTimer.Stop();
            gameTime = false;
            if (proposedAcronyms.Count == 0)
            {
                client.SendMessage(SendType.Message, "#cooking", "Acronym: " + currentAcronym + ". Nobody gave any meanings.");
                return;
            }

            client.SendMessage(SendType.Message, "#cooking", "Acronym: " + currentAcronym + ". Time is up! Listing the proposed meanings.");

            for(int index = 0; index < proposedAcronyms.Count; index++)
            {
                AcronymProposal acronymProposal = proposedAcronyms[index];

                string finalString = "Vote !" + (index+1) + " " + acronymProposal.acronym + " [" + acronymProposal.timeSpanString + "]";
                client.SendMessage(SendType.Message, "#cooking", finalString);
            }

            client.SendMessage(SendType.Message, "#cooking", "Listing over. Time to vote!");
            client.SendMessage(SendType.Message, "#cooking", "Vote by saying the trigger !x as seen above.");

            votingTime = true;
            voteTimer.Start();
        }

        void endGame(object sender, ElapsedEventArgs e)
        {
            voteTimer.Stop();
            client.SendMessage(SendType.Message, "#cooking", "Voting is over.  The results are in:");

            Globals globals = Globals.getInstance();
            KeyValuePair<string, int> winner = new KeyValuePair<string,int>(String.Empty, -1);
            string totalsString = String.Empty;
            List<KeyValuePair<string, int>> ties = new List<KeyValuePair<string, int>>();
            foreach (KeyValuePair<string, int> vote in voteTotals)
            {
                string nick = vote.Key;

                if (vote.Value > 0)
                {
                    if (winner.Key != String.Empty && winner.Value != -1) // if winner is not null
                    {
                        if (vote.Value > winner.Value)
                        {
                            winner = vote;

                            if (ties.Count != 0)
                            {
                                Console.WriteLine("Ties cleared. New winner: " + vote.Key + " for " + vote.Value + " votes.");
                                ties.Clear();
                            }
                        }

                        else if (vote.Value == winner.Value)
                        {
                            ties.Add(vote);
                            Console.WriteLine("Tie added, between last winner and this vote");
                            Console.WriteLine("Tied for score: " + ties[0].Value);
                            foreach (KeyValuePair<string, int> tie in ties)
                            {
                                Console.WriteLine("Tie name: " + tie.Key);
                            }
                            Console.WriteLine("Total ties: " + ties.Count);
                        }
                    }
                    else
                    {
                        winner = vote;
                    }
                    
                }

                totalsString = nick + " with " + vote.Value + " votes. [Lifetime score: ";
                if (globals.lifetimePoints.ContainsKey(nick))
                    totalsString += globals.lifetimePoints[nick];
                else
                    totalsString += "0";

                totalsString += " points.]";

                client.SendMessage(SendType.Message, "#cooking", totalsString);
            }

            if (ties.Count != 0)
            {
                string tieString = "A tie between: ";

                for (int index = 0; index < ties.Count; index++)
                {
                    tieString += ties[index].Key;

                    if (index == (ties.Count - 2))
                        tieString += " and ";
                    else
                        tieString += ", ";
                }

                tieString += ".  [" + ties[0].Value + " votes]";

                client.SendMessage(SendType.Message, "#cooking", tieString);
            }
            else
            {
                client.SendMessage(SendType.Message, "#cooking", "We have a winner: " + winner.Key + "!");
            }

            votingTime = false;
            proposedAcronyms.Clear();
            peopleWhoVotedAndForWho.Clear();
            voteTotals.Clear();
        }

        private void generateNewAcronym()
        {
            currentAcronym = String.Empty;
            
            int length = random.Next(minLength, maxLength);
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
            string nick = e.Data.Nick;
            string message = e.Data.Message;
            Globals globals = Globals.getInstance();
            if (!globals.IgnoredUsers.Contains(nick))
            {
                if (gameTime)
                {
                    AcronymProposal existingProposal = findProposalByName(nick);

                    string[] words = message.TrimStart(space).TrimEnd(space).Split(space);
                    int length = words.Length;
                    int expectedLength = currentAcronym.Length;
                    Console.WriteLine("Received new acronym from " + nick);

                    if (length != expectedLength)
                    {
                        Console.WriteLine("Acronym not the right length.");
                        client.SendMessage(SendType.Message, nick, "There is something wrong with the acronym you sent.  Expected " + expectedLength + " words.  You sent " + length + " word(s).");

                        if (existingProposal != null)
                            client.SendMessage(SendType.Message, nick, "Old acronym not replaced.");

                        return;
                    }
                    else
                    {
                        int index = -1;
                        foreach (string word in words)
                        {
                            index++;

                            char firstLetter = Char.ToLower(word[0]);
                            if (firstLetter == '\"' || firstLetter == '\'')
                                firstLetter = Char.ToLower(word[1]);

                            char expectedLetter = currentAcronym[index];

                            if (firstLetter != expectedLetter)
                            {
                                Console.WriteLine("Acronym has mismatched letters.");
                                client.SendMessage(SendType.Message, nick, "There is something wrong with the acronym you sent (mismatched letters). Please verify and re-send.");

                                if (existingProposal != null)
                                    client.SendMessage(SendType.Message, nick, "Old acronym not replaced.");

                                return;
                            }
                        }
                    }

                    TimeSpan timeSpan = DateTime.Now.Subtract(startTime);
                    string timeString = timeSpan.Seconds.ToString() + " seconds";

                    if (timeSpan.Minutes > 0)
                        timeString = timeSpan.Minutes.ToString() + " minute " + timeString;

                    Console.WriteLine("Received at: " + timeString);

                    AcronymProposal acronymProposal;
                    if (existingProposal != null)
                    {
                        Console.WriteLine("Removing existing one.");
                        client.SendMessage(SendType.Message, nick, "New acronym received. Replacing old one.");
                        existingProposal.timeSpanString = timeString;
                        existingProposal.acronym = message;
                        int proposalIndex = proposedAcronyms.IndexOf(existingProposal);
                        proposedAcronyms[proposalIndex] = existingProposal;
                    }
                    else
                    {
                        client.SendMessage(SendType.Message, nick, "Acronym received. Thank you.");
                        acronymProposal = new AcronymProposal();
                        acronymProposal.nickname = nick;
                        acronymProposal.timeSpanString = timeString;
                        acronymProposal.acronym = message;
                        proposedAcronyms.Add(acronymProposal);
                    }
                }

                else if (votingTime)
                {
                    if (message.TrimStart(' ').StartsWith("!"))
                    {
                        try
                        {
                            int vote = Convert.ToInt32(message.Substring(1));
                            vote--; // votes are inflated by one always, to make it easier to understand

                            if (vote < 0 || vote > (proposedAcronyms.Count - 1))
                                return;

                            if (peopleWhoVotedAndForWho.ContainsKey(nick))
                            {
                                Console.WriteLine(nick + " already voted (" + peopleWhoVotedAndForWho[nick] + ")");

                                int oldVote = peopleWhoVotedAndForWho[nick];
                                if (oldVote != vote)
                                {
                                    client.SendMessage(SendType.Message, nick, "New vote received.  Old one removed.");
                                    string nickOfProposerOld = proposedAcronyms[oldVote].nickname;
                                    Console.WriteLine(nick + "'s old vote was for " + nickOfProposerOld + ".  Getting rid of vote totals and Lifetime pointss");

                                    Console.WriteLine("Lifetime points for " + nickOfProposerOld + " before: " + globals.lifetimePoints[nickOfProposerOld]);
                                    Console.WriteLine("Voting score for " + nickOfProposerOld + " before: " + voteTotals[nickOfProposerOld]);

                                    globals.lifetimePoints[nickOfProposerOld]--;
                                    voteTotals[nickOfProposerOld]--;

                                    Console.WriteLine("Lifetime points for " + nickOfProposerOld + " after: " + globals.lifetimePoints[nickOfProposerOld]);
                                    Console.WriteLine("Voting score for " + nickOfProposerOld + " after: " + voteTotals[nickOfProposerOld]);
                                }
                                else
                                {
                                    client.SendMessage(SendType.Message, nick, "You already voted for the same dude.");
                                    return; // it will give them an extra vote if we don't return here
                                }
                            }
                            else
                                client.SendMessage(SendType.Message, nick, "Vote received.  Thank you.");
                            

                            peopleWhoVotedAndForWho[nick] = vote;
                            string nickOfProposer = proposedAcronyms[vote].nickname;
                            Console.WriteLine(nick + " is voting for " + nickOfProposer + " [#" + (vote + 1) + "]");

                            bool hasOverallScore = globals.lifetimePoints.ContainsKey(nickOfProposer);
                            bool hasVotes = voteTotals.ContainsKey(nickOfProposer);

                            if (hasOverallScore)
                            {
                                Console.WriteLine("Lifetime points: " +
                                                  globals.lifetimePoints[nickOfProposer] +
                                                  " + 1 = " +
                                                  ++globals.lifetimePoints[nickOfProposer]);
                            }
                            else
                            {
                                Console.WriteLine("Lifetime points: 0 + 1 = 1");
                                globals.lifetimePoints[nickOfProposer] = 1;
                            }

                            if (hasVotes)
                            {
                                Console.WriteLine("Votes: " +
                                                  voteTotals[nickOfProposer] +
                                                  " + 1 = " +
                                                  ++voteTotals[nickOfProposer]);

                            }
                            else
                            {
                                Console.WriteLine("Votes: 0 + 1 = 1");
                                voteTotals[nickOfProposer] = 1;
                            }
                        }
                        catch (Exception)  {   }
                    }
                }
            }
        }

        private AcronymProposal findProposalByName(string nick)
        {
            AcronymProposal existingProposal = null;

            if (proposedAcronyms.Count != 0)
            {
                AcronymProposal proposal;
                for (int index = 0; index < proposedAcronyms.Count; index++)
                {
                    proposal = proposedAcronyms[index];
                    if (proposal.nickname == nick)
                    {
                        existingProposal = proposal;
                        break;
                    }
                }
            }

            return existingProposal;
        }
    }
}
