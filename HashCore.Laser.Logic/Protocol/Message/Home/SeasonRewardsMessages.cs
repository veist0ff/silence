namespace HashCore.Laser.Logic.Message.Home
{
    using HashCore.Laser.Logic.Home.Challenges;
    using HashCore.Laser.Logic.Home.Items;

    /// <summary>
    /// Запрос наград, 12938. GetSeasonRewardsMessage::decode (0x1004bcfd4) —
    /// один VInt, ключ. Окно испытания шлёт 4, 5 или 6 по ChallengeId
    /// (ChampionshipChallengePopup, 0x1001338b8), экран бойца — 1.
    /// </summary>
    public class GetSeasonRewardsMessage : GameMessage
    {
        public int Key;

        public override void Decode()
        {
            Key = Stream.ReadVInt();
        }

        public override int GetMessageType() => 12938;
        public override int GetServiceNodeType() => 9;
    }

    /// <summary>
    /// Ответ, 22033. SeasonRewardsMessage::decode (0x1004caad4): два
    /// необязательных LogicGemOffer (+168, +176), список RewardEntry (у каждой
    /// флаг «есть»), затем ключ (+144). Клиент кладёт ответ под этот ключ, и
    /// пока ответа нет, окно испытания не строит этапы (createEvents выходит).
    ///
    /// RewardEntry::decode (0x1004ca664): VInt +0 — номер победы, VInt +4,
    /// VInt +8, булево +48, LogicGemOffer +16 без флага, затем три
    /// необязательных LogicGemOffer (+24, +32, +40).
    /// </summary>
    public class SeasonRewardsMessage : GameMessage
    {
        public int Key;
        public List<ChallengeWinReward> Rewards = new List<ChallengeWinReward>();

        public override void Encode()
        {
            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false);

            Stream.WriteVInt(Rewards.Count);
            for (int i = 0; i < Rewards.Count; i++)
            {
                ChallengeWinReward reward = Rewards[i];
                Stream.WriteBoolean(true);
                Stream.WriteVInt(reward.Win);
                Stream.WriteVInt(0);
                Stream.WriteVInt(0);
                // +48 — последняя, главная награда.
                Stream.WriteBoolean(i == Rewards.Count - 1);
                (reward.Offer ?? new LogicGemOffer()).Encode(Stream);
                Stream.WriteBoolean(false);
                Stream.WriteBoolean(false);
                Stream.WriteBoolean(false);
            }

            Stream.WriteVInt(Key);
        }

        public override int GetMessageType() => 22033;
        public override int GetServiceNodeType() => 9;
    }
}
