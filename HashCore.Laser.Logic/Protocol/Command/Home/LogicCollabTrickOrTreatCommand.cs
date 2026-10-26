namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Home;

    /// <summary>
    /// «Сладость или гадость» Brawloween, тип 600 (LogicCollabTrickOrTreatCommand).
    /// decode: bool «гадость» (+28), bool «сладость» (+29).
    /// Гадость: −TrickCost конфет (поле коллаба +136), выдать PumpkinBox (TRICK_BOX).
    /// Сладость: −цена уровня, уровень ящика (+164 у игрока) +1, максимум — число цен.
    /// Коды как в canExecute: 2 нет данных игрока, 4/5 мало конфет, 6 максимум.
    /// </summary>
    public class LogicCollabTrickOrTreatCommand : Command
    {
        public const int TrickCost = 100;
        public const int TreatCost = 100;
        public const int TreatMaxLevel = HashCore.Laser.Logic.Home.Items.EventData.DefaultTreatLevels;

        public bool Trick;
        public bool Treat;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            Trick = stream.ReadBoolean();
            Treat = stream.ReadBoolean();
        }

        public override int Execute(HomeMode homeMode)
        {
            ClientHome home = homeMode?.Home;
            if (home == null) return 1;
            lock (home.RewardSync)
            {
                if (Trick)
                {
                    if (home.CollabCurrency < TrickCost) return 4;
                    home.CollabCurrency -= TrickCost;
                    var box = HashCore.Laser.Logic.Home.RandomReward.RandomRewardManager.GetContainer("PumpkinBox");
                    if (box != null) HashCore.Laser.Logic.Home.RandomReward.RandomRewardManager.Give(homeMode, box);
                    Server.Logger.Print($"[Treat] гадость: −{TrickCost} конфет, PumpkinBox, осталось {home.CollabCurrency}");
                }
                else if (Treat)
                {
                    if (home.CollabTreatLevel >= TreatMaxLevel) return 6;
                    if (home.CollabCurrency < TreatCost) return 5;
                    home.CollabCurrency -= TreatCost;
                    home.CollabTreatLevel++;
                    Server.Logger.Print($"[Treat] сладость: уровень {home.CollabTreatLevel}, осталось {home.CollabCurrency}");
                }
                home.RefreshCollabSnapshot(System.DateTime.UtcNow);
            }
            return 0;
        }

        public override int GetCommandType() => 600;
    }
}
