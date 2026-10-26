namespace HashCore.Laser.Logic.Home.Calendar
{
    using System;
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Home.Gatcha;
    using HashCore.Laser.Logic.Home.Items;
    using HashCore.Laser.Logic.Message.Home;

    public class LoginCalendarRewardOption
    {
        public LogicGemOffer Offer = new LogicGemOffer();
        public bool HasTrackChoice;
        public int TrackId;

        public void Encode(ByteStream stream)
        {
            Offer.Encode(stream);
            stream.WriteBoolean(HasTrackChoice);
            if (HasTrackChoice) stream.WriteVInt(TrackId);
        }

        public void Decode(ByteStream stream)
        {
            Offer = new LogicGemOffer();
            Offer.Type = (ShopItem)stream.ReadVInt();
            Offer.Count = stream.ReadVInt();
            Offer.ItemDataId = ByteStreamHelper.ReadDataReference(stream);
            Offer.SkinDataId = stream.ReadVInt();

            HasTrackChoice = stream.ReadBoolean();
            if (HasTrackChoice) TrackId = stream.ReadVInt();
        }

        public GatchaDrop CreateDrop()
        {
            int type;
            switch (Offer.Type)
            {
                case ShopItem.Gems: type = 8; break;
                case ShopItem.PowerPoints: type = 24; break;
                case ShopItem.RecruitToken: type = 22; break;
                case ShopItem.Bling: type = 25; break;
                case ShopItem.Coin: type = 7; break;
                default: return null;
            }

            GatchaDrop drop = new GatchaDrop(type);
            drop.Count = Offer.Count;
            drop.DataGlobalId = Offer.ItemDataId;
            return drop;
        }
    }
    public class LoginCalendarDay
    {
        public int DayNumber;
        public bool Highlighted;
        public readonly List<LoginCalendarRewardOption> Options = new List<LoginCalendarRewardOption>();

        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(DayNumber);
            stream.WriteBoolean(Highlighted);

            stream.WriteVInt(Options.Count);
            foreach (LoginCalendarRewardOption option in Options)
            {
                option.Encode(stream);
            }
        }

        public void Decode(ByteStream stream)
        {
            DayNumber = stream.ReadVInt();
            Highlighted = stream.ReadBoolean();

            Options.Clear();
            for (int i = stream.ReadVInt(); i > 0; i--)
            {
                LoginCalendarRewardOption option = new LoginCalendarRewardOption();
                option.Decode(stream);
                Options.Add(option);
            }
        }
    }
    public class LoginCalendarTrack
    {
        public int TrackId;
        public readonly List<LoginCalendarDay> Days = new List<LoginCalendarDay>();

        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(TrackId);

            stream.WriteVInt(Days.Count);
            foreach (LoginCalendarDay day in Days)
            {
                day.Encode(stream);
            }
        }

        public void Decode(ByteStream stream)
        {
            TrackId = stream.ReadVInt();

            Days.Clear();
            for (int i = stream.ReadVInt(); i > 0; i--)
            {
                LoginCalendarDay day = new LoginCalendarDay();
                day.Decode(stream);
                Days.Add(day);
            }
        }
    }
    public class LoginCalendar
    {
        public const int MAIN_TRACK_ID = 1;
        public const int BONUS_TRACK_ID = 2;

        public const int STREAK_SLOT = 3;
        public const int SLOT_COUNT = 4;

        public const int MAIN_TRACK_DAYS = 7;
        public const int TOTAL_DAYS = 7;
        public const int NEXT_DAY_HOURS = 20;

        public const int STREAK_LOST_HOURS = 24;

        public const int KEEP_STREAK_MAX_COST = 20;

        public LoginCalendarTrack MainTrack = new LoginCalendarTrack();
        public readonly List<LoginCalendarTrack> Tracks = new List<LoginCalendarTrack>();

        public int ClaimLimit;
        public int CurrentDay;
        public int NextDayTime;
        public int EndTime;
        public int ClaimedDay = -1;
        public int ChosenTrackId;

        public int CalendarId;
        public int Unk60;
        public int Unk64;
        public int Unk68;
        public bool IsDailyStreak;
        public bool StreakBlocked;
        public int StreakRefreshTime;
        public bool Unk80;
        public int Unk84;

        public LoginCalendarDay GetDay(int index)
        {
            if (index < 0) return null;
            if (index < MainTrack.Days.Count) return MainTrack.Days[index];

            int offset = index - MainTrack.Days.Count;
            if (Tracks.Count == 0 || offset >= Tracks[0].Days.Count) return null;
            return Tracks[0].Days[offset];
        }
        public void Encode(ByteStream stream)
        {
            MainTrack.Encode(stream);

            stream.WriteVInt(Tracks.Count);
            foreach (LoginCalendarTrack track in Tracks)
            {
                track.Encode(stream);
            }

            stream.WriteVInt(ClaimLimit);
            stream.WriteVInt(CurrentDay);
            stream.WriteVInt(NextDayTime);
            stream.WriteVInt(EndTime);
            stream.WriteVInt(ClaimedDay);
            stream.WriteVInt(ChosenTrackId);
            stream.WriteVInt(CalendarId);
            stream.WriteVInt(Unk60);
            stream.WriteVInt(Unk64);
            stream.WriteVInt(Unk68);
            stream.WriteBoolean(IsDailyStreak);
            stream.WriteBoolean(StreakBlocked);
            stream.WriteVInt(StreakRefreshTime);
            stream.WriteBoolean(Unk80);
            stream.WriteVInt(Unk84);
        }

        public void Decode(ByteStream stream)
        {
            MainTrack = new LoginCalendarTrack();
            MainTrack.Decode(stream);

            Tracks.Clear();
            for (int i = stream.ReadVInt(); i > 0; i--)
            {
                LoginCalendarTrack track = new LoginCalendarTrack();
                track.Decode(stream);
                Tracks.Add(track);
            }

            ClaimLimit = stream.ReadVInt();
            CurrentDay = stream.ReadVInt();
            NextDayTime = stream.ReadVInt();
            EndTime = stream.ReadVInt();
            ClaimedDay = stream.ReadVInt();
            ChosenTrackId = stream.ReadVInt();
            CalendarId = stream.ReadVInt();
            Unk60 = stream.ReadVInt();
            Unk64 = stream.ReadVInt();
            Unk68 = stream.ReadVInt();
            IsDailyStreak = stream.ReadBoolean();
            StreakBlocked = stream.ReadBoolean();
            StreakRefreshTime = stream.ReadVInt();
            Unk80 = stream.ReadBoolean();
            Unk84 = stream.ReadVInt();
        }
        private static readonly ShopItem[] REWARD_TYPES =
        {
            ShopItem.Coin, ShopItem.PowerPoints, ShopItem.RecruitToken, ShopItem.Bling,
            ShopItem.Coin, ShopItem.PowerPoints, ShopItem.Gems
        };

        private static readonly int[] REWARD_COUNTS =
        {
            500, 50, 50, 25,
            1000, 150, 50
        };

        private static LoginCalendarRewardOption CreateOption(int position)
        {
            int slot = position % TOTAL_DAYS;

            LoginCalendarRewardOption option = new LoginCalendarRewardOption();
            option.Offer.Type = REWARD_TYPES[slot];
            option.Offer.Count = REWARD_COUNTS[slot];
            return option;
        }

        private static int GetPosition(ClientHome home)
        {
            int streak = home.LoginCalendarStreak;
            if (streak < 0) streak = 0;
            return streak % TOTAL_DAYS;
        }

        private static void RefreshStreak(ClientHome home)
        {
            if (home.LoginCalendarNextDay <= DateTime.UnixEpoch) return;

            DateTime deadline = DateTime.SpecifyKind(home.LoginCalendarNextDay, DateTimeKind.Utc)
                .AddHours(STREAK_LOST_HOURS);

            if (DateTime.UtcNow <= deadline) return;

            home.LoginCalendarStreak = 0;
            home.LoginCalendarNextDay = DateTime.UnixEpoch;
        }

        public static LoginCalendar Build(ClientHome home, int index)
        {
            RefreshStreak(home);

            LoginCalendar calendar = new LoginCalendar();
            calendar.MainTrack.TrackId = MAIN_TRACK_ID;

            // createItems dereferences tracks[0] without a null check.
            LoginCalendarTrack bonus = new LoginCalendarTrack();
            bonus.TrackId = BONUS_TRACK_ID;
            calendar.Tracks.Add(bonus);
            calendar.ChosenTrackId = BONUS_TRACK_ID;

            int position = GetPosition(home);

            for (int day = 0; day < TOTAL_DAYS; day++)
            {
                LoginCalendarDay entry = new LoginCalendarDay();
                entry.DayNumber = day;
                entry.Highlighted = day == TOTAL_DAYS - 1;
                entry.Options.Add(CreateOption(position + day));

                if (day < MAIN_TRACK_DAYS) calendar.MainTrack.Days.Add(entry);
                else bonus.Days.Add(entry);
            }

            DateTime now = DateTime.UtcNow;
            DateTime nextDay = DateTime.SpecifyKind(home.LoginCalendarNextDay, DateTimeKind.Utc);
            bool ready = nextDay <= now;

            int nextIn = ready ? 0 : (int)(nextDay - now).TotalSeconds;
            int claimWindow = STREAK_LOST_HOURS * 60 * 60;

            calendar.ClaimLimit = TOTAL_DAYS - 1;
            calendar.CurrentDay = 0;
            calendar.ClaimedDay = ready ? -1 : 0;
            calendar.NextDayTime = nextIn;
            calendar.EndTime = nextIn + claimWindow;

            // LoginCalendarUtil filters the slots on +72 and reports +68 as the slot index.
            calendar.IsDailyStreak = true;
            calendar.CalendarId = 1;
            calendar.Unk68 = index;
            calendar.StreakRefreshTime = nextIn;
            calendar.Unk84 = nextIn + claimWindow;

            return calendar;
        }
        public static int Claim(HomeMode homeMode, int day, int optionIndex, int calendarIndex)
        {
            ClientHome home = homeMode.Home;
            if (home == null || homeMode.Avatar == null) return 2;

            LoginCalendar calendar = Build(home, STREAK_SLOT);
            if (day != calendar.CurrentDay || day <= calendar.ClaimedDay) return 3;

            LoginCalendarDay entry = calendar.GetDay(day);
            if (entry == null || optionIndex < 0 || optionIndex >= entry.Options.Count) return 4;

            GatchaDrop drop = entry.Options[optionIndex].CreateDrop();
            if (drop == null) return 5;

            drop.DoDrop(homeMode);

            home.LoginCalendarStreak++;
            home.LoginCalendarNextDay = DateTime.UtcNow.AddHours(NEXT_DAY_HOURS);

            SendDelivery(homeMode, drop);
            Send(homeMode, 1);
            return 0;
        }

        public static int KeepStreak(HomeMode homeMode, int index)
        {
            ClientHome home = homeMode?.Home;
            if (home == null || homeMode.Avatar == null) return 2;
            if (home.LoginCalendarStreak < 1) return 3;

            int cost = home.LoginCalendarStreak;
            if (cost > KEEP_STREAK_MAX_COST) cost = KEEP_STREAK_MAX_COST;
            if (!homeMode.Avatar.UseDiamonds(cost)) return 4;

            home.LoginCalendarNextDay = DateTime.UtcNow;

            SetState(homeMode, 2, index);
            Send(homeMode);
            return 0;
        }

        public static int ResetStreak(HomeMode homeMode, int index)
        {
            ClientHome home = homeMode?.Home;
            if (home == null) return 2;

            home.LoginCalendarStreak = 0;
            home.LoginCalendarNextDay = DateTime.UnixEpoch;

            SetState(homeMode, 2, index);
            Send(homeMode);
            return 0;
        }

        public static int SetState(HomeMode homeMode, int state, int index)
        {
            ClientHome home = homeMode.Home;
            if (home == null) return 2;
            if (index < 0 || index >= SLOT_COUNT) return 3;

            if (home.LoginCalendarStates == null || home.LoginCalendarStates.Length < SLOT_COUNT)
            {
                home.LoginCalendarStates = new int[SLOT_COUNT];
            }

            home.LoginCalendarStates[index] = state;
            return 0;
        }

        public static void Send(HomeMode homeMode, int streakDelta = 0)
        {
            if (homeMode?.GameListener == null || homeMode.Home == null) return;

            LogicLoginCalendarChangedCommand command = new LogicLoginCalendarChangedCommand();
            command.Calendar = Build(homeMode.Home, STREAK_SLOT);
            command.CalendarIndex = STREAK_SLOT;
            command.StreakFlag = streakDelta;

            AvailableServerCommandMessage message = new AvailableServerCommandMessage();
            message.Command = command;
            homeMode.GameListener.SendMessage(message);
        }

        private static void SendDelivery(HomeMode homeMode, GatchaDrop drop)
        {
            if (homeMode?.GameListener == null) return;

            DeliveryUnit unit = new DeliveryUnit(100);
            unit.AddDrop(drop);

            LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
            command.DeliveryUnits.Add(unit);

            AvailableServerCommandMessage message = new AvailableServerCommandMessage();
            message.Command = command;
            homeMode.GameListener.SendMessage(message);
        }
    }
}
