namespace HashCore.Laser.Logic.Avatar.Structures
{
    public class PlayerDisplayData
    {
        public int ThumbnailId;
        public int NameColorId;
        public string Name;

        public PlayerDisplayData()
        {
        }

        public PlayerDisplayData(int thumbnail, int namecolor, string name)
        {
            ThumbnailId = thumbnail;
            NameColorId = namecolor;
            Name = name;
        }

        public void Encode(ByteStream stream)
        {
            // Эталонный рабочий пакет: имя, 100, thumbnail 28001006, nameColor 43000000, 0.
            // Нулевой nameColor и -1 в четвёртом поле клиент не ждёт.
            stream.WriteString(Name);
            stream.WriteVInt(5000);
            stream.WriteVInt(ThumbnailId != 0 ? ThumbnailId : 28000000);
            stream.WriteVInt(NameColorId != 0 ? NameColorId : 43000000);
            stream.WriteVInt(0);
            stream.WriteBoolean(false);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
        }

        public void Decode(ByteStream stream)
        {
            Name = stream.ReadString();
            stream.ReadVInt();
            ThumbnailId = stream.ReadVInt();
            NameColorId = stream.ReadVInt();
            stream.ReadVInt();
            stream.ReadBoolean();
            stream.ReadVInt();
            stream.ReadVInt();
        }
    }
}
