namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Server;

    /// <summary>
    /// Покупка гира, тип 558.
    ///
    /// Формат снят с клиента (`LogicPurchaseGearCommand::decode` 0x1003cbae4):
    /// ссылка на бойца, ссылка на гир, VInt со слотом и булев признак «плачу
    /// самоцветами». В `execute` (0x1003cbce8) клиент зовёт `addHeroGear`, а затем
    /// `setHeroSelectedGear` — покупает и сразу ставит в указанный слот.
    ///
    /// На сервере команды не было, она падала в лог как «command 558 not found».
    /// </summary>
    public class LogicPurchaseGearCommand : Command
    {
        /// <summary>Боец, которому покупают гир.</summary>
        public int CharacterGlobalId;

        /// <summary>Гир, глобальный идентификатор таблицы 62.</summary>
        public int GearGlobalId;

        /// <summary>Слот: 0 — первый, 1 — второй.</summary>
        public int Slot;

        /// <summary>Игрок доплачивает самоцветами вместо монет.</summary>
        public bool PayWithGems;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            CharacterGlobalId = ByteStreamHelper.ReadDataReference(stream);
            GearGlobalId = ByteStreamHelper.ReadDataReference(stream);
            Slot = stream.ReadVInt();
            PayWithGems = stream.ReadBoolean();
        }

        public override int Execute(HomeMode homeMode)
        {
            if (homeMode?.Avatar == null) return -1;

            Hero hero = homeMode.Avatar.GetHero(CharacterGlobalId);
            if (hero == null)
            {
                Logger.Warning($"[purchase::gear] у игрока нет бойца {CharacterGlobalId}");
                return -1;
            }

            DataTable table = DataTables.Get(DataType.Gear);
            if (table == null || table.GetDataByGlobalId<GearData>(GearGlobalId) == null)
            {
                Logger.Warning($"[purchase::gear] гир {GearGlobalId} не найден");
                return -1;
            }

            if (hero.UnlockedGears == null) hero.UnlockedGears = new List<int>();
            if (!hero.UnlockedGears.Contains(GearGlobalId)) hero.UnlockedGears.Add(GearGlobalId);

            // Цену гира клиент считает сам в getGearFinalCoinPrice: формула по
            // редкости и уровню, и она пока не разобрана. Пока не списываем —
            // лучше не брать, чем взять неверно.

            int gearInstanceId = GlobalId.GetInstanceId(GearGlobalId);
            if (Slot == 1) hero.SelectedGearId2 = gearInstanceId;
            else hero.SelectedGearId1 = gearInstanceId;

            Logger.Print($"[purchase::gear] гир {GearGlobalId} куплен для {hero.CharacterData?.Name}, слот {Slot}");
            return 0;
        }

        public override int GetCommandType()
        {
            return 558;
        }
    }
}
