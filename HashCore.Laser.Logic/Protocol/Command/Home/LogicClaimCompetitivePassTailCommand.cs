namespace HashCore.Laser.Logic.Command.Home
{
    using System.Collections.Generic;

    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Server;

    /// <summary>
    /// Забрать хвост рангового пропуска, тип 594.
    ///
    /// Формат снят с клиента (`LogicClaimCompetitivePassTailCommand::decode`
    /// 0x1003c4320): два VInt — трек и сезон. В `execute` (0x1003c4524) клиент
    /// увеличивает счётчик забранных хвостов у сезона и выдаёт предложение типа 61
    /// (`CompetitivePassTailReward`) с сезоном в дополнительном поле.
    ///
    /// Сама награда лежит в competitive_pass_tiers.csv строкой с треком **3** на
    /// нужный сезон — берём её оттуда и выдаём тем же путём, что трофейная дорога.
    /// Хвост повторяемый, поэтому вместо отметки «забрано» здесь счётчик.
    /// </summary>
    public class LogicClaimCompetitivePassTailCommand : Command
    {
        public int TrackType;
        public int Season;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            TrackType = stream.ReadVInt();
            Season = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            if (homeMode?.Home == null || homeMode.Avatar == null) return 1;

            // Хвост в таблице лежит отдельным треком, номер ступени у него нулевой.
            RankedMilestones tier = RankedPassRewards.FindTier(3, Season, 0);
            if (tier == null)
            {
                Logger.Warning($"[rankedPass] хвоста нет для сезона {Season}");
                return 3;
            }

            if (!RankedPassRewards.Grant(homeMode, tier)) return 1;

            if (homeMode.Home.RankedPassTailClaimed == null)
                homeMode.Home.RankedPassTailClaimed = new Dictionary<int, int>();

            homeMode.Home.RankedPassTailClaimed.TryGetValue(Season, out int claimed);
            homeMode.Home.RankedPassTailClaimed[Season] = claimed + 1;

            Logger.Print($"[rankedPass] выдан хвост сезона {Season}, всего раз {claimed + 1}");
            return 0;
        }

        public override int GetCommandType()
        {
            return 594;
        }
    }
}
