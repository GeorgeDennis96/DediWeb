namespace DediBotWeb.Common.Models
{
    public class PlayerModel
    {
        public Guid InternalId { get; set; } = Guid.NewGuid();
        public decimal DiscordId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int TotalGamesPlayed => GetTotalGamesPlayed();
        public int Wins { get; set; }
        public int Losses { get; set; }
        public decimal WinRate => GetWinRate();
        public int Balance { get; set; }
        public DateTime DailyClaimedAt { get; set; }

        private int GetTotalGamesPlayed()
        {
            return Wins + Losses;
        }

        private decimal GetWinRate()
        {
            if (TotalGamesPlayed == 0) return 0;
            return Math.Round((decimal)Wins / TotalGamesPlayed * 100, 1);
        }
    }
}
