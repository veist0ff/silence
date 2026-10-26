namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Battle.Objects;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Helper;
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Structures;
     
    public class LogicSelectCharacterCommand : Command
    {
        public int CharacterInstanceId;
        public int Index;

        /// <summary>
        /// Формат по клиенту (0x1003d1298): ссылка на бойца и номер слота **одним
        /// сырым байтом** (слот 248 у ByteStream — это readInt8, не readVInt).
        /// </summary>
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            CharacterInstanceId = GlobalId.GetInstanceId(ByteStreamHelper.ReadDataReference(stream));
            Index = stream.ReadByte();
        }

        public override int Execute(HomeMode homeMode)
        {
            int globalId = GlobalId.CreateGlobalId(16, CharacterInstanceId);
            if (homeMode.Avatar.HasHero(globalId))
            {
                homeMode.Home.CharacterIds[Index] = globalId;
                homeMode.CharacterChanged.Invoke(globalId);
                Hero hero = homeMode.Avatar.GetHero(globalId);
                return 0;
            }

            return -1;
        }

        public override int GetCommandType()
        {
            return 525;
        }
    }
}
