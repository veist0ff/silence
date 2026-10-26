namespace HashCore.Laser.Logic.Home.Items
{
    using System.Collections.Generic;

    // EventData +256: decode 0x100500874, encode 0x100500674.
    // The imported decode name incorrectly identifies this as player data.
    public sealed class SpecialRushEvent
    {
        public List<int>? ExcludedTrophyGameSlots = new(); // +24, max 32
        public List<int>? OverrideSlots = new(); // +40, max 32
        public List<List<LogicGemOffer?>?>? OverrideSlotRewards = new(); // +56, outer max 32
        public int Unk0;
        public List<LogicGemOffer?>? Rewards = new(); // +8, max 32

        public void Encode(ByteStream w)
        {
            PlayerCollabEventData.WriteArray(w, ExcludedTrophyGameSlots, 32, w.WriteVInt);
            PlayerCollabEventData.WriteArray(w, OverrideSlots, 32, w.WriteVInt);
            PlayerCollabEventData.WriteArray(w, OverrideSlotRewards, 32, rewards =>
            {
                if (w.WriteBoolean(rewards != null))
                {
                    // The inner array has no fatal count limit in the client.
                    w.WriteVInt(rewards!.Count);
                    foreach (var reward in rewards)
                        if (w.WriteBoolean(reward != null)) reward!.Encode(w);
                }
            });
            w.WriteVInt(Unk0);
            PlayerCollabEventData.WriteArray(w, Rewards, 32, reward =>
            {
                if (w.WriteBoolean(reward != null)) reward!.Encode(w);
            });
        }
    }
}
