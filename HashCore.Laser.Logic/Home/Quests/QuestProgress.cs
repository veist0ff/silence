namespace HashCore.Laser.Logic.Home.Quests
{
    using System.Collections.Generic;

    using HashCore.Laser.Logic.Home;

    /// <summary>
    /// Продвижение заданий по итогам боя.
    ///
    /// Прогресс задания — поле +12 структуры QuestData, цель — +16. Клиент считает
    /// задание выполненным в `QuestData::isCompleted` (0x1004c91dc) как
    /// `+72 + +12 - +76 >= +16`; поля +72 и +76 сервер шлёт нулями, так что сравнение
    /// сводится к «прогресс не меньше цели». Раньше прогресс не двигался вообще:
    /// ни одна строка кода его не меняла, и в лобби у всех заданий стоял ноль.
    ///
    /// Тип задания — поле +8, и его значения это индексы в таблице строк
    /// TID_QUEST_TYPE_REQ_* (0x100d6d030): 0 победа, 1 убийство, 2 урон, 3 лечение,
    /// 4 сыграть бой, 5 командная игра, 6 режим.
    /// </summary>
    public static class QuestProgress
    {
        private const int TypeWin = 0;
        private const int TypeKill = 1;
        private const int TypeDealDamage = 2;
        private const int TypeHeal = 3;
        private const int TypePlayAgain = 4;
        private const int TypeTeamPlay = 5;
        private const int TypeModeSpecific = 6;

        /// <summary>
        /// Засчитывает один бой во все подходящие задания.
        /// Возвращает, сколько заданий сдвинулось.
        /// </summary>
        public static int RegisterBattle(
            HomeMode homeMode,
            bool victory,
            int gameModeVariation,
            int heroGlobalId,
            int kills,
            int damage,
            int heals)
        {
            ClientHome home = homeMode != null ? homeMode.Home : null;
            if (home == null || home.questsList == null) return 0;

            int moved = 0;

            foreach (Quest quest in home.questsList)
            {
                if (quest == null) continue;
                if (quest.QuestsProgress <= 0) continue;
                if (quest.Progress >= quest.QuestsProgress) continue;

                if (!MatchesMode(quest, gameModeVariation)) continue;
                if (!MatchesHero(quest, heroGlobalId)) continue;

                int gained = GainFor(quest.Missia, victory, kills, damage, heals);
                if (gained <= 0) continue;

                quest.Progress += gained;
                if (quest.Progress > quest.QuestsProgress) quest.Progress = quest.QuestsProgress;

                moved++;
            }

            return moved;
        }

        private static int GainFor(int type, bool victory, int kills, int damage, int heals)
        {
            switch (type)
            {
                case TypeWin:
                case TypeTeamPlay:
                case TypeModeSpecific:
                    return victory ? 1 : 0;

                case TypeKill:
                    return kills > 0 ? kills : 0;

                case TypeDealDamage:
                    return damage > 0 ? damage : 0;

                case TypeHeal:
                    return heals > 0 ? heals : 0;

                case TypePlayAgain:
                    return 1;

                default:
                    return 0;
            }
        }

        /// <summary>Пустой список режимов — задание засчитывается в любом бою.</summary>
        private static bool MatchesMode(Quest quest, int gameModeVariation)
        {
            List<int> modes = quest.GameModes;

            if (modes == null || modes.Count == 0)
            {
                return quest.GameMode == 0 || quest.GameMode == gameModeVariation;
            }

            return modes.Contains(gameModeVariation);
        }

        /// <summary>Пустой список бойцов — задание засчитывается на любом.</summary>
        private static bool MatchesHero(Quest quest, int heroGlobalId)
        {
            List<int> heroes = quest.Heroes;

            if (heroes == null || heroes.Count == 0)
            {
                return quest.BrawlerId == 0 || quest.BrawlerId == heroGlobalId;
            }

            return heroes.Contains(heroGlobalId);
        }
    }
}
