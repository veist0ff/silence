namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Battle.Objects;
    using HashCore.Laser.Logic.Data;
     
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Structures;
    using HashCore.Laser.Logic.Team;
     
    public class LogicSelectSkinCommand : Command
    {
        int skinId;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt();
            skinId = stream.ReadVInt();
            stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            // Скин бойца, которого на сервере нет (новые/китайские скины), раньше ронял
            // обработку всего пакета команд NullReferenceException, и игрока выкидывало.
            SkinData skinData = DataTables.Get(DataType.Skin).GetData<SkinData>(skinId);
            SkinConfData skinConfData = skinData == null ? null : DataTables.Get(DataType.SkinConf).GetData<SkinConfData>(skinData.Conf);
            CharacterData character = skinConfData == null ? null : DataTables.Get(16).GetData<CharacterData>(skinConfData.Character);
            Hero hero = character == null ? null : homeMode.Avatar?.GetHero(character.GetInstanceId() + 16000000);
            if (hero == null)
            {
                if (character == null || homeMode.Avatar == null)
                {
                    HashCore.Laser.Server.Logger.Print($"[SelectSkin] скин {skinId}: нет данных скина/бойца — пропуск");
                    return 0;
                }
                // Бойца нет, но скин выбрать можно — запоминаем отдельно.
                homeMode.Avatar.SelectedSkinsWithoutHero ??= new();
                homeMode.Avatar.SelectedSkinsWithoutHero[character.GetInstanceId() + 16000000] = skinId;
                return 0;
            }
            hero.SelectedSkinId = skinId;
            homeMode.CharacterChanged?.Invoke(0);
            return 0;
        }

        public override int GetCommandType()
        {
            return 506;
        }
    }
}
