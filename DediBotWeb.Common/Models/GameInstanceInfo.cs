using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DediBotWeb.Common.Models
{
    public class GameInstanceInfo
    {
        public GameInstanceInfo(int initialNumber, SocketUser initialChallenger, SocketUser challengedUser)
        {
            InitialNumber = initialNumber;
            InitialChallenger = initialChallenger;
            ChallengedUser = challengedUser;

            SetupButtons();
        }

        public Guid ID { get; private set; } = Guid.NewGuid();
        public string AcceptButtonID { get; set; }
        public string DeclineButtonID { get; set; }
        public SocketUser InitialChallenger { get; private set; }
        public SocketUser ChallengedUser { get; private set; }
        public int InitialNumber { get; private set; }
        public SocketUser WhoRollsNext { get; private set; }
        public List<Roll> RollHistory { get; private set; } = new List<Roll>();

        public GameInstanceInfo AddRollHistory(int newNumber, ulong whoRolled)
        {
            var roll = new Roll(whoRolled, newNumber);
            RollHistory.Add(roll);
            WhoRollsNext = SetTurn(whoRolled);

            Console.WriteLine($"{whoRolled} rolled a {roll.RolledNumber}. Between 1 - {newNumber}");

            return this;
        }

        public SocketUser SetTurn(ulong lastPlayed)
        {
            if (lastPlayed == InitialChallenger.Id)
            {
                return ChallengedUser;
            }
            else
            {
                return InitialChallenger;
            }
        }

        private void SetupButtons()
        {
            // Create the unique id for each button..
            AcceptButtonID = $"accept-{Guid.NewGuid()}";
            DeclineButtonID = $"decline-{Guid.NewGuid()}";
        }
    }
}
