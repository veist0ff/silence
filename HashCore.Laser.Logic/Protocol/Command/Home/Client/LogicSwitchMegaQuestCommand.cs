namespace HashCore.Laser.Logic.Command.Home
{
    using System.Collections.Generic;

    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Items;

    /// <summary>
    /// Смена мега-задания, тип 979.
    ///
    /// Формат по клиенту (`LogicSwitchMegaQuestCommand::decode`, 0x1003d4114):
    /// заголовок, затем ДВА необязательных задания — каждое предваряется булевым,
    /// и только при истине идёт тело `QuestData`. Первым в потоке лежит новое
    /// задание (поле +40), вторым — старое (+32).
    ///
    /// Тело задания разбирается в <see cref="ClientQuestData"/> — без него поток
    /// съезжал бы на сотни байт.
    /// </summary>
    public class LogicSwitchMegaQuestCommand : Command
    {
        /// <summary>Задание, на которое меняем. Может отсутствовать.</summary>
        public ClientQuestData NewQuest;

        /// <summary>Задание, которое меняем. Может отсутствовать.</summary>
        public ClientQuestData OldQuest;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            if (stream.ReadBoolean())
            {
                NewQuest = new ClientQuestData();
                NewQuest.Decode(stream);
            }

            if (stream.ReadBoolean())
            {
                OldQuest = new ClientQuestData();
                OldQuest.Decode(stream);
            }
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 979;
        }
    }

    /// <summary>
    /// Задание в том виде, в каком его присылает клиент (`QuestData::decode`,
    /// 0x1004c96bc). Порядок полей странный — он идёт не по возрастанию смещений,
    /// — но менять его нельзя: это ровно та последовательность, которую пишет и
    /// читает клиент.
    ///
    /// Длины всех списков клиент ограничивает 1024 записями
    /// («Too many items in array decode»), повторяем и это.
    /// </summary>
    public class ClientQuestData
    {
        private const int MaxItems = 1024;

        public List<int> EventSlots = new List<int>();
        public List<int> Gmvs = new List<int>();
        public List<int> Heroes = new List<int>();
        public List<LogicGemOffer> Rewards = new List<LogicGemOffer>();
        public List<LogicGemOffer> FallbackRewards = new List<LogicGemOffer>();

        public bool Flag141;
        public bool Flag140;
        public bool Flag142;
        public bool Flag144;
        public bool Flag145;
        public bool Flag81;
        public bool Flag80;
        public bool FlagTail;

        public int Value76;
        public int Value4;
        public int Value72;
        public int Value128;
        public int Value164;
        public int Value152;
        public int Value156;
        public int Value132;
        public int Value148;
        public int Value16;
        public int Value0;
        public int Value8;
        public int Value136;
        public int Value20;
        public int Value160;
        public int Value124;
        public int Value120;
        public int Value12;

        public void Decode(ByteStream stream)
        {
            ReadInts(stream, EventSlots);
            Flag141 = stream.ReadBoolean();

            ReadInts(stream, Gmvs);

            Value76 = stream.ReadVInt();
            Value4 = stream.ReadVInt();
            Value72 = stream.ReadVInt();
            Value128 = stream.ReadVInt();
            Flag140 = stream.ReadBoolean();
            Value164 = stream.ReadVInt();
            Flag142 = stream.ReadBoolean();
            Value152 = stream.ReadVInt();
            Value156 = stream.ReadVInt();

            int heroes = Limit(stream.ReadVInt());
            for (int i = 0; i < heroes; i++) Heroes.Add(ByteStreamHelper.ReadDataReference(stream));

            Flag144 = stream.ReadBoolean();
            Flag145 = stream.ReadBoolean();
            Flag81 = stream.ReadBoolean();
            Value132 = stream.ReadVInt();

            ReadOffers(stream, Rewards);
            ReadOffers(stream, FallbackRewards);

            Value148 = stream.ReadVInt();
            Value16 = stream.ReadVInt();
            Value0 = stream.ReadVInt();
            Value8 = stream.ReadVInt();
            Value136 = stream.ReadVInt();
            Value20 = stream.ReadVInt();
            Value160 = stream.ReadVInt();
            Value124 = stream.ReadVInt();
            Value120 = stream.ReadVInt();
            Value12 = stream.ReadVInt();

            Flag80 = stream.ReadBoolean();
            FlagTail = stream.ReadBoolean();
        }

        private static void ReadInts(ByteStream stream, List<int> into)
        {
            int count = Limit(stream.ReadVInt());
            for (int i = 0; i < count; i++) into.Add(stream.ReadVInt());
        }

        /// <summary>
        /// Награда в задании необязательная: сначала булево, и только при истине
        /// идёт само предложение.
        /// </summary>
        private static void ReadOffers(ByteStream stream, List<LogicGemOffer> into)
        {
            int count = Limit(stream.ReadVInt());

            for (int i = 0; i < count; i++)
            {
                if (!stream.ReadBoolean()) continue;

                LogicGemOffer offer = new LogicGemOffer();
                offer.Decode(stream);
                into.Add(offer);
            }
        }

        private static int Limit(int count)
        {
            if (count < 0) return 0;
            return count > MaxItems ? MaxItems : count;
        }
    }
}
