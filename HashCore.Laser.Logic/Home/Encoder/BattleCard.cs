namespace HashCore.Laser.Logic.Home.Structures
{
    using HashCore.Laser.Logic.Avatar;
    using Newtonsoft.Json;
    using HashCore.Laser.Logic.Avatar.Structures;
    using HashCore.Laser.Logic.Data;

    using HashCore.Laser.Titan.Math;

    [JsonObject(MemberSerialization.OptOut)]
    public class BattleCard
    {
        public int Thumbnail1;
        public int Thumbnail2;
        public int Emote;
        public int Title;

         public int Frames;

        public BattleCard()
        {
            ;
        }

        public void Encode(ByteStream stream)
        {
            ByteStreamHelper.WriteDataReference(stream, null);
            ByteStreamHelper.WriteDataReference(stream, Safe(Thumbnail1));
            ByteStreamHelper.WriteDataReference(stream, Safe(Thumbnail2));
            ByteStreamHelper.WriteDataReference(stream, Safe(Emote));
            ByteStreamHelper.WriteDataReference(stream, Safe(Title));
            ByteStreamHelper.WriteDataReference(stream, Safe(Frames));

            stream.WriteBoolean(Thumbnail1 == 0);
            stream.WriteBoolean(Thumbnail2 == 0);
            stream.WriteBoolean(Emote == 0);
            stream.WriteBoolean(Title == 0);

            stream.WriteBoolean(Frames == 0);
        }

        // LogicHeroBattleIntro::decode резолвит каждую ссылку через getDataById.
        // Ссылка на несуществующую строку валит клиента при сборке домашнего экрана.
        private static int Safe(int globalId)
        {
            if (globalId <= 0) return 0;

            DataTable table = DataTables.Get(GlobalId.GetClassId(globalId));
            if (table == null) return globalId;

            return table.GetDataByGlobalId<LogicData>(globalId) != null ? globalId : 0;
        }

    }
}
