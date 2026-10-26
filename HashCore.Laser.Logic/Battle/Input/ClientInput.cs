namespace HashCore.Laser.Logic.Battle.Input
{
     
    using HashCore.Laser.Logic.Data.Helper;
  
    public class ClientInput
    {
        public ClientInput()
        {
            ;
        }

        public int Index;
        public int Type;
        public int EmoteIndex;

        public int X, Y;

        public bool AutoAttack;
        public int AutoAttackTarget; // global id

        // ClientInput +48: instance id скилла для типов 0/1/18 (LogicDataTables::getSkillByInstanceID)
        public int SkillInstanceId = -1;

        public long OwnerSessionId;

        public void Decode(BitStream Stream)
        {
            Index = Stream.ReadPositiveInt(15);
            //Debugger.Print(Index.ToString());
            Type = Stream.ReadPositiveInt(5);

            X = Stream.ReadInt(15);
            Y = Stream.ReadInt(15);
            Stream.ReadBoolean();
            AutoAttack = Stream.ReadBoolean();
            Stream.ReadBoolean();
            // ClientInput::decode 0x1004b6280: типы 0 (атака), 1 (ульта) и 18
            // несут флаг + 9 бит instance id скилла. Без этого битстрим уезжает.
            if (Type == 0 || Type == 1 || Type == 18)
            {
                if (Stream.ReadBoolean())
                {
                    SkillInstanceId = Stream.ReadPositiveIntMax511();
                }
            }
            if (Type == 9) // use emote
            {
                // writePositiveIntMax15 = 4 бита (BitStream::readPositiveIntMax15 0x100503a78)
                EmoteIndex = Stream.ReadPositiveIntMax15();
            }
            if (Type == 11) // use emote
            {
                Stream.ReadPositiveVIntMax255(); // emote index
            }
            if (Type == 15) // use emote
            {
                EmoteIndex = Stream.ReadPositiveIntMax511()-6; // emote index
                Stream.ReadPositiveIntMax511(); // emote index
            }
            if (Type == 16) // use emote
            {
                Stream.ReadPositiveIntMax511(); 
                //Stream.ReadPositiveIntMax511(); 
            }
            // ClientInput::encode 0x1004b60c8: тип 21 тоже несёт writePositiveVIntMax255.
            // Без него весь хвост списка вводов в пакете читался со сдвигом, и сервер
            // применял к персонажу мусорные Index/Type/X/Y.
            if (Type == 21) // выбор карточки рогалика: номер среди предложенных (CombatHUD::selectPowerUp)
            {
                EmoteIndex = Stream.ReadPositiveVIntMax255();
            }
            if (AutoAttack)
            {
                if (Stream.ReadBoolean())
                {
                    int v7 = Stream.ReadPositiveInt(14); // global id
                    AutoAttackTarget = GlobalId.CreateGlobalId(1, v7);
                }
            }
        }
    }
}
