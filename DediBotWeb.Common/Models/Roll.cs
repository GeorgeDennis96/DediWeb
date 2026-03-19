namespace DediBotWeb.Common.Models
{
    public class Roll
    {
        public ulong WhoRolled { get; private set; }
        public int RolledNumber { get; private set; }

        public Roll(ulong whoRolled, int maxValue)
        {
            WhoRolled = whoRolled;
            RolledNumber = Random.Shared.Next(1, maxValue);
        }
    }
}
