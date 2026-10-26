namespace HashCore.Laser.Logic.Command.Home
{
     
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Structures;
    
    public class LogicLevelUpCommand : Command
    {
        private int CharacterId;

        /// <summary>Повышать до упора, а не на один уровень.</summary>
        private bool Maximum;

        /// <summary>
        /// Формат по клиенту (`LogicLevelUpCommand::decode`): ссылка на бойца и
        /// булево. Хвостовое булево сервер не читал, из-за чего поток съезжал на
        /// байт и следующая команда в пакете разбиралась мусором.
        /// </summary>
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            CharacterId = ByteStreamHelper.ReadDataReference(stream);
            Maximum = stream.ReadBoolean();
        }

        public override int Execute(HomeMode homeMode)
        {
            Hero hero = homeMode.Avatar.GetHero(CharacterId);
            if (hero == null) return -1;

            if (hero.PowerLevel > Hero.UpgradeCostTable.Length) return -2;
            if (hero.PowerPoints < Hero.UpgradePowerPointsTable[hero.PowerLevel]) return -3;

            if (!homeMode.Avatar.UseGold(Hero.UpgradeCostTable[hero.PowerLevel])) return -4;

            hero.PowerLevel++;

            return 0;
        }

        public override int GetCommandType()
        {
            return 520;
        }
    }
}
