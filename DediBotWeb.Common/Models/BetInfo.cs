using Discord.WebSocket;

namespace DediBotWeb.Common.Models
{
    public class BetInfo
    {
        public int Amount { get; private set; }
        public SocketUser Bettor { get; private set; }




        public BetInfo(int amount, SocketUser bettor)
        {
            Amount = amount;
            Bettor = bettor;


        }
    }
}
