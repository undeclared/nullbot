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
        private const double GAME_LENGTH_MINUTES = 2;
        private const double VOTE_LENGTH_MINUTES = 1;
        private const string LETTERS = "abcdefghijlmnoprstuvwy";

        private bool customAcronym;
        private bool gameTime;
        private bool votingTime;
        private Timer gameTimer;
        private Timer voteTimer;
        private string currentAcronym;
        private Random random;
        private DateTime startTime;
        private List<AcronymProposal> proposedAcronyms;
        private SerializableDictionary<string, int> peopleWhoVotedAndForWho;
        private SerializableDictionary<string, int> voteTotals;
        private int minLength;
        private int maxLength;

        public AcronymGame() : base("Acronym Game")
        {
            gameTime = false;
            votingTime = false;
            customAcronym = false;
            random = new Random();
            gameTimer = new Timer(GAME_LENGTH_MINUTES * 60 * 1000);
            gameTimer.Elapsed += startVote;
            voteTimer = new Timer(VOTE_LENGTH_MINUTES * 60 * 1000);
            voteTimer.Elapsed += endGame;
            proposedAcronyms = new List<AcronymProposal>();
            peopleWhoVotedAndForWho = new SerializableDictionary<string, int>();
            voteTotals = new SerializableDictionary<string, int>();
            client.OnQueryMessage += client_OnQueryMessage;
            client.OnChannelMessage += client_OnChannelMessage;

            minLength = MIN_LENGTH_DEFAULT;
            maxLength = MAX_LENGTH_DEFAULT;
        }

        void client_OnChannelMessage(object sender, IrcEventArgs e)
        {
            string message = e.Data.Message;
            string nick = e.Data.Nick;

            if (message.StartsWith("!acronym") &&
                !globalStorage.IgnoredUsers.Contains(nick) &&
                !gameTime &&
                !votingTime)
            {
                customAcronym = false;

                string[] commands = message.Split(' ');

                if (commands.Length == 2)
                {
                    bool isNum = Int32.TryParse(commands[1], out minLength);

                    if (isNum)
                    {
                        if (minLength < 4)
                        {
                            minLength = MIN_LENGTH_DEFAULT;
                            client.SendMessage(SendType.Message, "#cooking", "Invalid length specified. Must be at least 4.");
                            return;
                        }
                        else
                            maxLength = minLength;
                    }
                    else
                    {
                        if (commands[1] != String.Empty)
                        {

                            customAcronym = true;
                            foreach (char ch in commands[1])
                            {
                                if (!Char.IsLetter(ch))
                                {
                                    customAcronym = false;
                                    client.SendMessage(SendType.Message, e.Data.Channel, "Invalid letters detected! Fix it.");
                                    return;
                                }
                            }
                            currentAcronym = commands[1].ToLower();
                        }
                    }
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
            client.SendMessage(SendType.Message, "#cooking", "New acronym: " + currentAcronym + ". /msg " + client.Nickname + " [acronym]. Results voted on in 2 minutes if 2+ entries.");
            log.VerboseMessage("Generated new acronym: " + currentAcronym);
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

            else if (proposedAcronyms.Count == 1)
            {
                client.SendMessage(SendType.Message, "#cooking", "Acronym: " + currentAcronym + ". Time is up! Listing the proposed meanings.");
                client.SendMessage(SendType.Message, "#cooking", "(" + proposedAcronyms[0].nickname + ") " + proposedAcronyms[0].acronym + " [" + proposedAcronyms[0].timeSpanString + "]");
                client.SendMessage(SendType.Message, "#cooking", "Thanks for playing. No voting is available due to only one entry.");
                
                votingTime = false;
                proposedAcronyms.Clear();
                peopleWhoVotedAndForWho.Clear();
                voteTotals.Clear();
            }
            else
            {
                client.SendMessage(SendType.Message, "#cooking", "Acronym: " + currentAcronym + ". Time is up! Listing the proposed meanings.");
                for(int index = 0; index < proposedAcronyms.Count; index++)
                {
                    AcronymProposal acronymProposal = proposedAcronyms[index];

                    string finalString = "Vote !" + (index+1) + " " + acronymProposal.acronym + " [" + acronymProposal.timeSpanString + "]";
                    client.SendMessage(SendType.Message, "#cooking", finalString);
                }

                client.SendMessage(SendType.Message, "#cooking", "Listing over. Time to vote!");
                client.SendMessage(SendType.Message, "#cooking", "/msg " + client.Nickname + " !x to vote for which number. Vote lasts 1 minute.");

                votingTime = true;
                voteTimer.Start();
            }
        }

        void endGame(object sender, ElapsedEventArgs e)
        {
            voteTimer.Stop();
            client.SendMessage(SendType.Message, "#cooking", "Voting is over.  The results are in:");

            GlobalStorage globals = GlobalStorage.getInstance();
            KeyValuePair<string, int> winner = new KeyValuePair<string,int>(String.Empty, -1);
            string totalsString = String.Empty;
            List<string> ties = new List<string>();
            foreach (KeyValuePair<string, int> currentVotee in voteTotals)
            {
                string nick = currentVotee.Key;
                int votes = currentVotee.Value;
                int lastWinnerVotes = winner.Value;

                if (votes > 0)
                {
                    if (winner.Key != String.Empty && winner.Value != -1) // if winner is not default/basicly null since I can't make this null
                    {
                        if (votes > lastWinnerVotes) 
                        {
                            winner = currentVotee; // the new winner is this guy, because the last guy had less points

                            if (ties.Count != 0) // if there's ties, there is no more anymore
                            {
                                log.VerboseMessage("Ties cleared. New winner: " + nick + " for " + votes + " votes.");
                                ties.Clear();
                            }
                        }
                        else if (votes == lastWinnerVotes) // the last winner had the same score as this guy
                        {
                            if (ties.Count == 0) // lets add the previous winner to ties, too.. 
                                ties.Add(winner.Key);

                            ties.Add(nick); // lets add this guy to ties then
                            
                            log.VerboseMessage("Tie added, between last winner and this vote");
                            log.VerboseMessage("Tied for score: " + votes);
                            foreach (string tie in ties)
                            {
                                log.VerboseMessage("Tie name: " + tie);
                            }
                            log.VerboseMessage("Total ties: " + ties.Count);
                        }
                    }
                    else
                        winner = currentVotee;
                }

                totalsString = nick + " with " + currentVotee.Value + " votes. [Lifetime score: ";
                
                if (globals.lifetimePoints.ContainsKey(nick))
                    totalsString += globals.lifetimePoints[nick];
                else
                    totalsString += "0";

                totalsString += " points.]";

                totalsString += " [" + findProposalByName(nick).acronym + "]";

                client.SendMessage(SendType.Message, "#cooking", totalsString);
            }

            if (ties.Count != 0)
            {
                string tieString = "A tie between: ";

                tieString += String.Join(", ", ties);

                client.SendMessage(SendType.Message, "#cooking", tieString);
            }
            else if (winner.Key == String.Empty)
            {
                client.SendMessage(SendType.Message, "#cooking", "Assholes! Nobody voted.");
            }
            else
                client.SendMessage(SendType.Message, "#cooking", "We have a winner: " + winner.Key + "!");

            votingTime = false;
            proposedAcronyms.Clear();
            peopleWhoVotedAndForWho.Clear();
            voteTotals.Clear();
        }

        private void generateNewAcronym()
        {
            if (customAcronym)
                return;

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
            GlobalStorage globals = GlobalStorage.getInstance();
            if (!globals.IgnoredUsers.Contains(nick))
            {
                if (gameTime)
                {
                    AcronymProposal existingProposal = findProposalByName(nick);
                    
                    message = message.TrimStart(space).TrimEnd(space);
                    message = message.Replace("  ", " ");

                    string[] words = message.Split(space);
                    int length = words.Length;
                    int expectedLength = currentAcronym.Length;
                    log.VerboseMessage("Received new acronym from " + nick);

                    if (length != expectedLength)
                    {
                        log.VerboseMessage("Acronym not the right length.");
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
                            if (firstLetter == '\"' || firstLetter == '\'' || firstLetter == '(')
                                firstLetter = Char.ToLower(word[1]);

                            char expectedLetter = currentAcronym[index];

                            if (firstLetter != expectedLetter)
                            {
                                log.VerboseMessage("Acronym has mismatched letters.");
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

                    log.VerboseMessage("Received at: " + timeString);

                    AcronymProposal acronymProposal;
                    if (existingProposal != null)
                    {
                        log.VerboseMessage("Removing existing one.");
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
                                log.DebugMessage(nick + " already voted (" + peopleWhoVotedAndForWho[nick] + ")");

                                int oldVote = peopleWhoVotedAndForWho[nick];
                                if (oldVote != vote)
                                {
                                    client.SendMessage(SendType.Message, nick, "New vote received.  Old one removed.");
                                    string nickOfProposerOld = proposedAcronyms[oldVote].nickname;
                                    log.DebugMessage(nick + "'s old vote was for " + nickOfProposerOld + ".  Getting rid of vote totals and Lifetime pointss");

                                    log.DebugMessage("Lifetime points for " + nickOfProposerOld + " before: " + globals.lifetimePoints[nickOfProposerOld]);
                                    log.DebugMessage("Voting score for " + nickOfProposerOld + " before: " + voteTotals[nickOfProposerOld]);

                                    globals.lifetimePoints[nickOfProposerOld]--;
                                    voteTotals[nickOfProposerOld]--;

                                    log.DebugMessage("Lifetime points for " + nickOfProposerOld + " after: " + globals.lifetimePoints[nickOfProposerOld]);
                                    log.DebugMessage("Voting score for " + nickOfProposerOld + " after: " + voteTotals[nickOfProposerOld]);
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
                            log.VerboseMessage(nick + " is voting for " + nickOfProposer + " [#" + (vote + 1) + "]");

                            bool hasOverallScore = globals.lifetimePoints.ContainsKey(nickOfProposer);
                            bool hasVotes = voteTotals.ContainsKey(nickOfProposer);

                            if (hasOverallScore)
                            {
                                log.DebugMessage("Lifetime points: " +
                                                  globals.lifetimePoints[nickOfProposer] +
                                                  " => " +
                                                  ++globals.lifetimePoints[nickOfProposer]);
                            }
                            else
                            {
                                log.DebugMessage("Lifetime points: 0 => 1");
                                globals.lifetimePoints[nickOfProposer] = 1;
                            }

                            if (hasVotes)
                            {
                                log.DebugMessage("Votes: " +
                                                  voteTotals[nickOfProposer] +
                                                  " => " +
                                                  ++voteTotals[nickOfProposer]);

                            }
                            else
                            {
                                log.DebugMessage("Votes: 0 => 1");
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
