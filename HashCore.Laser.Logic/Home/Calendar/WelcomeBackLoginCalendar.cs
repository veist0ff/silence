namespace HashCore.Laser.Logic.Home.Calendar
{
    using System;
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Avatar;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Home.Items;
    using HashCore.Laser.Logic.Message.Home;

    public class WelcomeBackBrawlerEntry
    {
        public int CharacterGlobalId;
        public int LevelUps;
        public bool IsNew;

        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(LevelUps);
            stream.WriteBoolean(IsNew);
            ByteStreamHelper.WriteDataReference(stream, CharacterGlobalId);
        }

        public void Decode(ByteStream stream)
        {
            LevelUps = stream.ReadVInt();
            IsNew = stream.ReadBoolean();
            CharacterGlobalId = ByteStreamHelper.ReadDataReference(stream);
        }
    }

    public class WelcomeBackLoginCalendarEntry
    {
        public const int MAX_BRAWLERS = 5;

        public int NotificationId;

        public int EventId = 1;

        public int SourceId;

        public readonly List<LogicGemOffer> Offers = new List<LogicGemOffer>();
        public readonly List<WelcomeBackBrawlerEntry> Brawlers = new List<WelcomeBackBrawlerEntry>();

        public int RewardDataId;

        public int Unk64;

        public int GetTotalLevelUps()
        {
            int total = Offers.Count;
            foreach (WelcomeBackBrawlerEntry brawler in Brawlers)
            {
                total += brawler.LevelUps;
            }

            return total;
        }

        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(NotificationId);
            stream.WriteVInt(EventId);
            stream.WriteVInt(SourceId);

            stream.WriteVInt(Offers.Count);
            foreach (LogicGemOffer offer in Offers)
            {
                offer.Encode(stream);
            }

            stream.WriteVInt(Brawlers.Count);
            foreach (WelcomeBackBrawlerEntry brawler in Brawlers)
            {
                brawler.Encode(stream);
            }

            ByteStreamHelper.WriteDataReference(stream, RewardDataId);
            stream.WriteVInt(Unk64);

            stream.WriteBoolean(false);
            stream.WriteBoolean(false);
        }

        public void Decode(ByteStream stream)
        {
            NotificationId = stream.ReadVInt();
            EventId = stream.ReadVInt();
            SourceId = stream.ReadVInt();

            Offers.Clear();
            for (int i = stream.ReadVInt(); i > 0; i--)
            {
                LogicGemOffer offer = new LogicGemOffer();
                offer.Type = (ShopItem)stream.ReadVInt();
                offer.Count = stream.ReadVInt();
                offer.ItemDataId = ByteStreamHelper.ReadDataReference(stream);
                offer.SkinDataId = stream.ReadVInt();
                Offers.Add(offer);
            }

            Brawlers.Clear();
            for (int i = stream.ReadVInt(); i > 0; i--)
            {
                WelcomeBackBrawlerEntry brawler = new WelcomeBackBrawlerEntry();
                brawler.Decode(stream);
                Brawlers.Add(brawler);
            }

            RewardDataId = ByteStreamHelper.ReadDataReference(stream);
            Unk64 = stream.ReadVInt();

            if (stream.ReadBoolean())
            {
                stream.ReadVInt();
                stream.ReadVInt();
            }

            if (stream.ReadBoolean())
            {
                stream.ReadVInt();
                stream.ReadVInt();
            }
        }
    }

    public static class WelcomeBackLoginCalendar
    {
        public const int AWAY_DAYS = 2;
        public const int BRAWLER_COUNT = 3;
        public const int MAX_POWER_LEVEL = 11;

        public static WelcomeBackLoginCalendarEntry Build(HomeMode homeMode)
        {
            ClientAvatar avatar = homeMode?.Avatar;
            if (avatar == null) return null;

            WelcomeBackLoginCalendarEntry entry = new WelcomeBackLoginCalendarEntry();
            int limit = BRAWLER_COUNT < WelcomeBackLoginCalendarEntry.MAX_BRAWLERS
                ? BRAWLER_COUNT
                : WelcomeBackLoginCalendarEntry.MAX_BRAWLERS;

            foreach (int brawler in ClientHome.DefaultUnlockStarRoad)
            {
                if (entry.Brawlers.Count >= limit) break;
                if (avatar.HasHero(brawler)) continue;
                if (DataTables.Get(16).GetDataByGlobalId<CharacterData>(brawler) == null) continue;

                entry.Offers.Add(new LogicGemOffer(ShopItem.Brawler, 1, brawler));

                WelcomeBackBrawlerEntry display = new WelcomeBackBrawlerEntry();
                display.CharacterGlobalId = brawler;
                display.IsNew = true;
                entry.Brawlers.Add(display);
            }

            if (entry.GetTotalLevelUps() < 1) return null;
            return entry;
        }

        public static bool TrySend(HomeMode homeMode, int calendarIndex)
        {
            ClientHome home = homeMode?.Home;
            if (home == null || homeMode.GameListener == null) return false;

            DateTime previous = DateTime.SpecifyKind(home.PreviousVisitHomeTime, DateTimeKind.Utc);
            if (previous <= DateTime.UnixEpoch) return false;
            if (DateTime.UtcNow - previous < TimeSpan.FromDays(AWAY_DAYS)) return false;

            WelcomeBackLoginCalendarEntry entry = Build(homeMode);
            if (entry == null) return false;

            Send(homeMode, entry, calendarIndex);
            Grant(homeMode, entry);
            return true;
        }

        public static void Send(HomeMode homeMode, WelcomeBackLoginCalendarEntry entry, int calendarIndex)
        {
            if (homeMode?.GameListener == null || homeMode.Home == null) return;

            LogicLoginCalendarChangedCommand command = new LogicLoginCalendarChangedCommand();
            command.Calendar = LoginCalendar.Build(homeMode.Home, calendarIndex);
            command.WelcomeBack = entry;
            command.CalendarIndex = calendarIndex;

            AvailableServerCommandMessage message = new AvailableServerCommandMessage();
            message.Command = command;
            homeMode.GameListener.SendMessage(message);
        }

        public static void Grant(HomeMode homeMode, WelcomeBackLoginCalendarEntry entry)
        {
            ClientAvatar avatar = homeMode?.Avatar;
            if (avatar == null || entry == null) return;

            foreach (LogicGemOffer offer in entry.Offers)
            {
                if (offer.ItemDataId <= 0) continue;
                if (offer.Type != ShopItem.Brawler && (int)offer.Type != 30) continue;
                if (avatar.HasHero(offer.ItemDataId)) continue;

                avatar.UnlockHero(offer.ItemDataId);
            }

            foreach (WelcomeBackBrawlerEntry brawler in entry.Brawlers)
            {
                if (brawler.IsNew || brawler.LevelUps < 1) continue;

                Hero hero = avatar.GetHero(brawler.CharacterGlobalId);
                if (hero == null) continue;

                int target = hero.PowerLevel + brawler.LevelUps;
                if (target > MAX_POWER_LEVEL) target = MAX_POWER_LEVEL;
                if (target > hero.PowerLevel) hero.PowerLevel = target;
            }
        }
    }
}
