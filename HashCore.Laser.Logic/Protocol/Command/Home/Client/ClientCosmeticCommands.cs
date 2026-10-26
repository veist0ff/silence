namespace HashCore.Laser.Logic.Command.Home
{
    using System.Collections.Generic;

    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Data.Helper;

    /*
     * Косметика, пассивки и задания. Форматы сняты с клиента.
     */

    /// <summary>
    /// Режим случайного скина у бойца, тип 547. Ссылка на бойца и булево.
    /// </summary>
    public class LogicRandomSkinModeCommand : Command
    {
        public int CharacterGlobalId;
        public bool Enabled;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            CharacterGlobalId = ByteStreamHelper.ReadDataReference(stream);
            Enabled = stream.ReadBoolean();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 547;
        }
    }

    /// <summary>
    /// Выбор спрея, тип 555. Ссылка на спрей и номер слота.
    /// </summary>
    public class LogicSelectSprayCommand : Command
    {
        public int SprayGlobalId;
        public int Slot;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            SprayGlobalId = ByteStreamHelper.ReadDataReference(stream);
            Slot = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            if (homeMode?.Home == null || SprayGlobalId <= 0) return 1;
            
            homeMode.Home.SelectedSprays ??= new Dictionary<int, int>();
            homeMode.Home.SelectedSprays[Slot] = SprayGlobalId;
            return 0;
        }

        public override int GetCommandType()
        {
            return 555;
        }
    }

    /// <summary>
    /// Выбор рамки профиля, тип 577. Ссылка на рамку и булево.
    /// Сервер уже хранит список открытых рамок, поэтому выбранную тоже запоминаем
    /// и заодно добавляем в открытые, если её там нет.
    /// </summary>
    public class LogicSetPlayerFrameCommand : Command
    {
        public int FrameGlobalId;
        public bool Flag;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            FrameGlobalId = ByteStreamHelper.ReadDataReference(stream);
            Flag = stream.ReadBoolean();
        }

        public override int Execute(HomeMode homeMode)
        {
            if (homeMode?.Home == null || FrameGlobalId <= 0) return 1;
            
            homeMode.Home.UnlockedFrames ??= new List<int>();
            if (!homeMode.Home.UnlockedFrames.Contains(FrameGlobalId)) homeMode.Home.UnlockedFrames.Add(FrameGlobalId);
            
            homeMode.Home.SelectedFrameId = FrameGlobalId;
            return 0;
        }

        public override int GetCommandType()
        {
            return 577;
        }
    }

    /// <summary>
    /// Выбор пассивной способности аватара, тип 597.
    /// Ссылка на данные и сырой int — номер слота.
    /// </summary>
    public class LogicSelectAvatarPassiveCommand : Command
    {
        public int PassiveGlobalId;
        public int Slot;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            PassiveGlobalId = ByteStreamHelper.ReadDataReference(stream);
            Slot = stream.ReadInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 597;
        }
    }

    /// <summary>
    /// Снятие пассивной способности аватара, тип 598. Ссылка на данные.
    /// </summary>
    public class LogicUnselectAvatarPassiveCommand : Command
    {
        public int PassiveGlobalId;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            PassiveGlobalId = ByteStreamHelper.ReadDataReference(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 598;
        }
    }

    /// <summary>
    /// Выбор бонусного навыка, тип 599. Ссылка на данные.
    /// </summary>
    public class LogicSelectBonusSkillCommand : Command
    {
        public int SkillGlobalId;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            SkillGlobalId = ByteStreamHelper.ReadDataReference(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 599;
        }
    }

    /// <summary>
    /// Перекатить задание, тип 554. Одно число — номер задания.
    /// </summary>
    public class LogicRerollQuestCommand : Command
    {
        public int QuestIndex;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            QuestIndex = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 554;
        }
    }

    /// <summary>
    /// Действие в «Рекордах», тип 752.
    ///
    /// Формат по клиенту (`LogicRecordsActionCommand::decode`): три числа, затем
    /// список просмотренных уведомлений (длина и столько же чисел) и булево.
    /// Клиент обрезает список по 1024 записям, длиннее не бывает.
    /// </summary>
    public class LogicRecordsActionCommand : Command
    {
        public int Action;
        public int Value;
        public List<int> SeenNotifications = new List<int>();
        public bool Flag;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            Action = stream.ReadVInt();
            Value = stream.ReadVInt();

            int count = stream.ReadVInt();
            if (count > 1024) count = 1024;

            for (int i = 0; i < count; i++) SeenNotifications.Add(stream.ReadVInt());

            Flag = stream.ReadBoolean();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 752;
        }
    }

    /// <summary>
    /// Отладочная команда, тип 1946.
    ///
    /// Формат: ссылка на данные, строка (до 4096), ещё ссылка и два числа.
    /// Сервер её не выполняет — но обязан вычитать, иначе поток съедет.
    /// </summary>
    public class LogicDebugCommand : Command
    {
        public int FirstGlobalId;
        public string Text;
        public int SecondGlobalId;
        public int First;
        public int Second;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            FirstGlobalId = ByteStreamHelper.ReadDataReference(stream);
            Text = stream.ReadStringReference(4096);
            SecondGlobalId = ByteStreamHelper.ReadDataReference(stream);
            First = stream.ReadVInt();
            Second = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 1946;
        }
    }

}
