namespace HashCore.Laser.Logic.Home.Items
{
    using System;
    using System.Collections.Generic;

    // Wire order from LogicPlayerCollabEventData::decode (0x1003ba670).
    // Unk suffixes are decimal client object offsets, not protocol field numbers.
    public class PlayerCollabEventData
    {
        public int Unk48;
        public List<int>? SkillRewardWins = new();                    // +56, max 32
        public List<CollabEventRewardMilestone?>? PendingScoreRewards = new(); // +24, max 512
        public int Unk164;
        public int Unk108;
        public bool Unk44;
        public int Unk52;
        public int Unk96;
        public int Unk104;
        public int Unk20;
        public int Unk124;
        public int Unk120;
        public int Unk16;
        public List<int>? RogueliteCompletedBrawlers = new();          // +184, max 256
        public int Unk112;
        public int Unk100;
        public int Unk12;
        public List<int>? AllianceRewardedMilestones = new();         // +128, max 32
        public List<CollabEventDeliveredMilestoneReward?>? DeliveredAllianceRewards = new(); // +144, max 32
        public int Unk88;                                            // data reference
        public int Unk172;
        public int Unk40;
        public int Unk8;
        public int Unk168;
        public bool Unk160;
        public List<int>? RogueliteCompletedBrawlerAmounts = new();   // +200, max 256
        public List<int>? WinTokenEventSlots = new();                 // +72, max 32
        public int Unk116;
        public int Unk176;

        public void Encode(ByteStream w)
        {
            w.WriteVInt(Unk48);
            WriteArray(w, SkillRewardWins, 32, w.WriteVInt);
            WriteArray(w, PendingScoreRewards, 512, item =>
            {
                if (w.WriteBoolean(item != null)) item!.Encode(w);
            });
            w.WriteVInt(Unk164);
            w.WriteVInt(Unk108);
            w.WriteBoolean(Unk44);
            w.WriteVInt(Unk52);
            w.WriteVInt(Unk96);
            w.WriteVInt(Unk104);
            w.WriteVInt(Unk20);
            w.WriteVInt(Unk124);
            w.WriteVInt(Unk120);
            w.WriteVInt(Unk16);
            WriteArray(w, RogueliteCompletedBrawlers, 256, w.WriteVInt);
            w.WriteVInt(Unk112);
            w.WriteVInt(Unk100);
            w.WriteVInt(Unk12);
            WriteArray(w, AllianceRewardedMilestones, 32, w.WriteVInt);
            WriteArray(w, DeliveredAllianceRewards, 32, item =>
            {
                if (w.WriteBoolean(item != null)) item!.Encode(w);
            });
            ByteStreamHelper.WriteDataReference(w, Unk88);
            w.WriteVInt(Unk172);
            w.WriteVInt(Unk40);
            w.WriteVInt(Unk8);
            w.WriteVInt(Unk168);
            w.WriteBoolean(Unk160);
            WriteArray(w, RogueliteCompletedBrawlerAmounts, 256, w.WriteVInt);
            WriteArray(w, WinTokenEventSlots, 32, w.WriteVInt);
            w.WriteVInt(Unk116);
            w.WriteVInt(Unk176);
        }

        internal static void WriteArray<T>(ByteStream w, List<T>? items, int max, Action<T> writeItem)
        {
            if (items != null && items.Count > max)
                throw new ArgumentOutOfRangeException(nameof(items), items.Count, $"Client array limit is {max}.");

            // Negative counts skip the array in the client decoder.
            w.WriteVInt(items?.Count ?? -1);
            if (items != null)
                foreach (T item in items) writeItem(item);
        }
    }

    // LogicCollabEventRewardMilestone::decode (0x1003b9dfc).
    public class CollabEventRewardMilestone
    {
        public List<LogicGemOffer?>? RewardWithFallbacks = new();      // +8, max 32
        public int Unk0;

        public void Encode(ByteStream w)
        {
            PlayerCollabEventData.WriteArray(w, RewardWithFallbacks, 32, offer =>
            {
                if (w.WriteBoolean(offer != null)) offer!.Encode(w);
            });
            w.WriteVInt(Unk0);
        }
    }

    // LogicCollabEventDeliveredMilestoneReward::decode (0x1003b9af4).
    public class CollabEventDeliveredMilestoneReward
    {
        public int Unk8;
        public LogicGemOffer? Unk0;

        public void Encode(ByteStream w)
        {
            w.WriteVInt(Unk8);
            if (w.WriteBoolean(Unk0 != null)) Unk0!.Encode(w);
        }
    }
}
