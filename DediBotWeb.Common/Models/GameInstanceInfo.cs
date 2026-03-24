using Discord.WebSocket;

namespace DediBotWeb.Common.Models
{
    public class GameInstanceInfo
    {
        public Guid ID { get; private set; } = Guid.NewGuid();
        public string AcceptButtonID { get; set; } = $"accept-{Guid.NewGuid()}";
        public string DeclineButtonID { get; set; } = $"decline-{Guid.NewGuid()}";
        public SocketUser InitialChallenger { get; private set; }
        public SocketUser ChallengedUser { get; private set; }
        public int InitialNumber { get; private set; }
        public SocketUser WhoRollsNext { get; private set; }
        public List<Roll> RollHistory { get; private set; } = new List<Roll>();
        public int PotentialWinnings { get; private set; }
        public int BetWinnings { get; private set; }
        public GameState State { get; private set; } = GameState.Pending;

        public GameInstanceInfo(int initialNumber, SocketUser initialChallenger, SocketUser challengedUser)
        {
            InitialNumber = initialNumber;
            InitialChallenger = initialChallenger;
            ChallengedUser = challengedUser;
        }

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

        public void SetState(GameState newState)
        {
            State = newState;

            Console.WriteLine($"Game {ID} state changed to {State}.");
        }

        public void AddBet(int amount)
        {
            BetWinnings += amount;
        }

        public void AddPotentialWinnings(int amount)
        {
            PotentialWinnings += amount;
        }

        public enum GameState
        {
            Pending,
            InProgress,
            Completed
        }
    }
}
