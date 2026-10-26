using System;
using Newtonsoft.Json;

namespace HashCore.Laser.Logic.Home.EarlyAccess
{
    // Mutations and reward queue commits are protected by ClientHome.RewardSync.
    public sealed class GigiEarlyAccessState
    {
        private readonly object sync = new object();
        [JsonProperty] public int Tickets { get; private set; }
        [JsonProperty] public DateTime LastTicketDayUtc { get; private set; }
        [JsonProperty] public bool BoxClaimed { get; private set; }
        [JsonProperty] public int Wins { get; private set; }
        [JsonProperty] public int Boxes { get; private set; }
        [JsonProperty] public DateTime LastBatchUtc { get; private set; }
        [JsonProperty] public System.Collections.Generic.HashSet<Guid> RewardedBattles { get; private set; } = new();

        public bool GrantDailyTicket(DateTime utcNow, int maximumTickets = 3, int batchSize = 3)
        {
            if (maximumTickets < 1) throw new ArgumentOutOfRangeException(nameof(maximumTickets));
            lock (sync)
            {
                var day = utcNow.Date;
                if (LastTicketDayUtc.Date >= day) return false;
                Tickets = Math.Min(maximumTickets, Tickets + batchSize);
                LastTicketDayUtc = day;
                LastBatchUtc = day;
                return true;
            }
        }

        public bool TryRecordWin(DateTime utcNow, int winsPerBox = 2)
        {
            if (winsPerBox < 1) throw new ArgumentOutOfRangeException(nameof(winsPerBox));
            lock (sync)
            {
                Wins++;
                if (Wins < winsPerBox) return true;
                Wins -= winsPerBox;
                Boxes++;
                LastBatchUtc = utcNow;
                return true;
            }
        }

        public bool TryConsumeBox()
        {
            lock (sync) { if (Boxes <= 0) return false; Boxes--; return true; }
        }

        public bool TryConsumeTicket()
        {
            lock (sync)
            {
                if (Tickets <= 0) return false;
                Tickets--;
                return true;
            }
        }

        public bool TryClaimBox()
        {
            lock (sync)
            {
                if (BoxClaimed) return false;
                BoxClaimed = true;
                return true;
            }
        }
    }
}
