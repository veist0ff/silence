namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Helper;
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Structures;
     
    public class LogicSelectGearCommand : Command
    {
        public int characterId;
        public int gearid;
        public int gearslot;

        /// <summary>Снять гир, а не поставить.</summary>
        public bool Unequip;

        /// <summary>
        /// Формат по клиенту (`LogicSelectGearBoostCommand::decode`, 0x1003d1510):
        /// ссылка на бойца, ссылка на гир, номер слота **сырым байтом** и булево.
        /// Сервер читал пять VInt-ов и не читал хвостовое булево — поток съезжал,
        /// и весь остаток хода клиента пропадал.
        /// </summary>
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            characterId = GlobalId.GetInstanceId(ByteStreamHelper.ReadDataReference(stream));
            gearid = GlobalId.GetInstanceId(ByteStreamHelper.ReadDataReference(stream));
            gearslot = stream.ReadByte();
            Unequip = stream.ReadBoolean();
        }

        public override int Execute(HomeMode homeMode)
        {
            int globalId = GlobalId.CreateGlobalId(16, characterId);
            if (homeMode.Avatar.HasHero(globalId))
            {
                Hero hero = homeMode.Avatar.GetHero(globalId);
                if (gearslot == 0)
                {
                    hero.SelectedGearId1 = gearid;
                }
                else
                {
                    hero.SelectedGearId2 = gearid;
                }
                homeMode.CharacterChanged.Invoke(0);
                return 0;
            }
            return 0;
        }

        public override int GetCommandType()
        {
            return 543;
        }
    }
}
