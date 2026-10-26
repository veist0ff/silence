
    using System.Collections.Generic;
using HashCore.Laser.Logic.Avatar;
using HashCore.Laser.Logic.Command;
using HashCore.Laser.Logic.Home;

public class LogicPurchaseBrawlPassProgressCommand : Command
    {
        private static readonly List<(int Index, int ProgressStart, int Progress, int Season)> levelData = new List<(int, int, int, int)>
        {
            (1, 0, 50, 22),
            (2, 50, 200, 22),
            (3, 250, 200, 22),
            (4, 450, 200, 22),
            (5, 650, 200, 22),
            (6, 850, 200, 22),
            (7, 1050, 200, 22),
            (8, 1250, 200, 22),
            (9, 1450, 200, 22),
            (10, 1650, 350, 22),
            (11, 2000, 950, 22),
            (12, 2950, 950, 22),
            (13, 3900, 950, 22),
            (14, 4850, 950, 22),
            (15, 5800, 950, 22),
            (16, 6750, 950, 22),
            (17, 7700, 950, 22),
            (18, 8650, 950, 22),
            (19, 9600, 950, 22),
            (20, 10550, 950, 22),
            (21, 11500, 950, 22),
            (22, 12450, 950, 22),
            (23, 13400, 950, 22),
            (24, 14350, 950, 22),
            (25, 15300, 950, 22),
            (26, 16250, 950, 22),
            (27, 17200, 950, 22),
            (28, 18150, 950, 22),
            (29, 19100, 950, 22),
            (30, 20050, 950, 22),
            (31, 21000, 950, 22),
            (32, 21950, 950, 22),
            (33, 22900, 950, 22),
            (34, 23850, 950, 22),
            (35, 24800, 950, 22),
            (36, 25750, 950, 22),
            (37, 26700, 950, 22),
            (38, 27650, 950, 22),
            (39, 28600, 950, 22),
            (40, 29550, 950, 22),
            (41, 30500, 950, 22),
            (42, 31450, 950, 22),
            (43, 32400, 950, 22),
            (44, 33350, 950, 22),
            (45, 34300, 950, 22),
            (46, 35250, 950, 22),
            (47, 36200, 950, 22),
            (48, 37150, 950, 22),
            (49, 38100, 950, 22),
            (50, 39050, 950, 22)
        };

        public int Unknown { get; set; }

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            Unknown = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            var player = homeMode.Avatar as ClientAvatar;

            if (player == null)
            {
                return 0;
            }

            // Проверяем, хватает ли у игрока алмазов
            if (!player.UseDiamonds(30))
            {
                // Не хватает алмазов — выходим
                return 0;
            }

            int currentTokens = player.HomeMode.Home.BrawlPassTokens;

            var nextLevel = levelData.Find(level => level.ProgressStart + level.Progress > currentTokens);

            if (nextLevel == default)
            {
                // Игрок уже достиг максимального уровня
                return 0;
            }

            int tokensNeeded = nextLevel.ProgressStart + nextLevel.Progress - currentTokens;

            // Добавляем нужное количество токенов
            player.AddTokens(tokensNeeded);

            return 0;
        }

        public override int GetCommandType()
        {
            return 536;
        }
    }
